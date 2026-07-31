using Microsoft.EntityFrameworkCore;

public record AlertaPendenteDto(int IdCliente, int IdRegraAlerta, int IdEmpresa, string? Nome, string? Empresa, string? Telefone, TipoAlerta Tipo, int DiasOffset, DateOnly DataVencimento);

public record VarreduraResultadoDto(int Enviados, int Falhas, List<AlertaPendenteDto> Detalhes);

/// <summary>
/// Lógica de varredura/envio de alertas de vencimento — usada tanto pelo job diário
/// (<see cref="AlertaBackgroundService"/>) quanto pelos endpoints administrativos de teste manual.
/// </summary>
public class AlertaJobService
{
    private readonly AppDbContext _db;
    private readonly WhatsAppService _whatsApp;
    private readonly IConfiguration _config;
    private readonly ILogger<AlertaJobService> _logger;

    public AlertaJobService(AppDbContext db, WhatsAppService whatsApp, IConfiguration config, ILogger<AlertaJobService> logger)
    {
        _db = db;
        _whatsApp = whatsApp;
        _config = config;
        _logger = logger;
    }

    public DateOnly HojeNoFuso() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ObterTimeZone()));

    /// <summary>Lista quem seria notificado hoje, sem enviar mensagem nem gravar histórico.</summary>
    public async Task<List<AlertaPendenteDto>> SimularAsync(CancellationToken ct)
    {
        var candidatos = await ObterCandidatosAsync(HojeNoFuso(), ct);

        return candidatos
            .Select(c => new AlertaPendenteDto(c.Cliente.Id, c.Regra.Id, c.Empresa.IdEmpresa!.Value, c.Cliente.Nome, c.Empresa.Nome, c.Cliente.Telefone, c.Regra.Tipo, c.Regra.DiasOffset, c.Cliente.DataVencimento))
            .ToList();
    }

    /// <summary>Envia o alerta imediatamente para um único cliente/regra (usado pelo botão "Enviar teste" do painel admin) e grava no histórico, evitando reenvio pelo job diário.</summary>
    public async Task<(bool Sucesso, string? Erro)> EnviarTesteAsync(int idCliente, int idRegraAlerta, CancellationToken ct)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == idCliente, ct)
            ?? throw new AppException("Cliente não encontrado.");

        var regra = await _db.RegrasAlerta.FirstOrDefaultAsync(r => r.Id == idRegraAlerta, ct)
            ?? throw new AppException("Regra de alerta não encontrada.");

        var empresa = await _db.ConfigEmpresas.FirstOrDefaultAsync(e => e.IdEmpresa == cliente.IdEmpresa, ct)
            ?? throw new AppException("Empresa do cliente não encontrada.");

        var mensagem = regra.Mensagem.Replace("{nome}", cliente.Nome ?? "");
        var (sucesso, erro) = await _whatsApp.EnviarAsync(cliente.Telefone ?? "", mensagem, empresa.WahaSession);

        _db.HistoricoNotificacoes.Add(new HistoricoNotificacao
        {
            IdEmpresa = cliente.IdEmpresa,
            IdCliente = cliente.Id,
            IdRegraAlerta = regra.Id,
            DataVencimentoReferencia = cliente.DataVencimento,
            DataEnvio = DateTime.UtcNow,
            Sucesso = sucesso,
            ErroMensagem = erro
        });

        await _db.SaveChangesAsync(ct);

        return (sucesso, erro);
    }

    /// <summary>Executa a varredura de verdade: envia as mensagens via WAHA e grava o histórico.</summary>
    public async Task<VarreduraResultadoDto> ExecutarAsync(CancellationToken ct)
    {
        var candidatos = await ObterCandidatosAsync(HojeNoFuso(), ct);
        var detalhes = new List<AlertaPendenteDto>();
        var relatoriosPorEmpresa = new Dictionary<int, (ConfigEmpresa Empresa, List<string> Linhas)>();
        var enviados = 0;
        var falhas = 0;

        foreach (var (empresa, cliente, regra) in candidatos)
        {
            var idEmpresa = empresa.IdEmpresa!.Value;
            var mensagem = regra.Mensagem.Replace("{nome}", cliente.Nome ?? "");
            var (sucesso, erro) = await _whatsApp.EnviarAsync(cliente.Telefone ?? "", mensagem, empresa.WahaSession);

            _db.HistoricoNotificacoes.Add(new HistoricoNotificacao
            {
                IdEmpresa = idEmpresa,
                IdCliente = cliente.Id,
                IdRegraAlerta = regra.Id,
                DataVencimentoReferencia = cliente.DataVencimento,
                DataEnvio = DateTime.UtcNow,
                Sucesso = sucesso,
                ErroMensagem = erro
            });

            detalhes.Add(new AlertaPendenteDto(cliente.Id, regra.Id, idEmpresa, cliente.Nome, empresa.Nome, cliente.Telefone, regra.Tipo, regra.DiasOffset, cliente.DataVencimento));

            if (sucesso)
            {
                enviados++;

                if (!relatoriosPorEmpresa.TryGetValue(idEmpresa, out var entrada))
                {
                    entrada = (empresa, new List<string>());
                    relatoriosPorEmpresa[idEmpresa] = entrada;
                }

                entrada.Linhas.Add(FormatarLinhaRelatorio(cliente.Nome, regra));
            }
            else
            {
                falhas++;
            }
        }

        await _db.SaveChangesAsync(ct);

        foreach (var (empresa, linhas) in relatoriosPorEmpresa.Values)
        {
            if (linhas.Count > 0 && !string.IsNullOrWhiteSpace(empresa.WhatsappDono))
            {
                var relatorio = string.Join("\n", linhas);
                await _whatsApp.EnviarAsync(empresa.WhatsappDono!, relatorio, empresa.WahaSession);
            }
        }

        return new VarreduraResultadoDto(enviados, falhas, detalhes);
    }

    private record AlertaCandidato(ConfigEmpresa Empresa, Cliente Cliente, RegraAlerta Regra);

    private async Task<List<AlertaCandidato>> ObterCandidatosAsync(DateOnly hoje, CancellationToken ct)
    {
        var candidatos = new List<AlertaCandidato>();
        var empresas = await _db.ConfigEmpresas.ToListAsync(ct);

        foreach (var empresa in empresas)
        {
            if (empresa.IdEmpresa is not int idEmpresa)
                continue;

            var regras = await _db.RegrasAlerta
                .Where(r => r.IdEmpresa == idEmpresa && r.Ativo)
                .ToListAsync(ct);

            if (regras.Count == 0)
                continue;

            if (string.IsNullOrWhiteSpace(empresa.WahaSession))
            {
                _logger.LogWarning("Empresa {IdEmpresa} tem regras ativas mas nenhuma sessão WAHA configurada — pulando.", idEmpresa);
                continue;
            }

            var clientes = await _db.Clientes
                .Where(c => c.IdEmpresa == idEmpresa && c.Ativo)
                .ToListAsync(ct);

            foreach (var regra in regras)
            {
                var dataAlvo = regra.Tipo switch
                {
                    TipoAlerta.AntesVencimento => hoje.AddDays(regra.DiasOffset),
                    TipoAlerta.NoDia => hoje,
                    TipoAlerta.AposVencimento => hoje.AddDays(-regra.DiasOffset),
                    _ => hoje
                };

                foreach (var cliente in clientes.Where(c => c.DataVencimento == dataAlvo))
                {
                    var jaEnviado = await _db.HistoricoNotificacoes.AnyAsync(h =>
                        h.IdCliente == cliente.Id &&
                        h.IdRegraAlerta == regra.Id &&
                        h.DataVencimentoReferencia == cliente.DataVencimento &&
                        h.Sucesso, ct);

                    if (jaEnviado)
                        continue;

                    candidatos.Add(new AlertaCandidato(empresa, cliente, regra));
                }
            }
        }

        return candidatos;
    }

    private static string FormatarLinhaRelatorio(string? nome, RegraAlerta regra) => regra.Tipo switch
    {
        TipoAlerta.AntesVencimento => $"{nome} vence em {regra.DiasOffset} dia(s)",
        TipoAlerta.NoDia => $"{nome} vence hoje",
        TipoAlerta.AposVencimento => $"{nome} vencido há {regra.DiasOffset} dia(s)",
        _ => $"{nome}"
    };

    private TimeZoneInfo ObterTimeZone()
    {
        var id = _config["Alertas:TimeZoneId"];
        if (string.IsNullOrWhiteSpace(id))
            return TimeZoneInfo.Utc;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
