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

    /// <summary>
    /// Executa a varredura de verdade: envia as mensagens via WAHA e grava o histórico.
    /// Cada empresa processa sua fila de forma independente (em paralelo com as outras), mas dentro da
    /// fila de uma mesma empresa os envios são espaçados por um intervalo aleatório de 30–60s — o WAHA
    /// é uma integração não oficial com o WhatsApp, e uma rajada de mensagens do mesmo número é o tipo
    /// de padrão que costuma disparar bloqueio.
    /// </summary>
    public async Task<VarreduraResultadoDto> ExecutarAsync(CancellationToken ct)
    {
        var candidatos = await ObterCandidatosAsync(HojeNoFuso(), ct);

        var tarefasPorEmpresa = candidatos
            .GroupBy(c => c.Empresa.IdEmpresa!.Value)
            .Select(grupo => ProcessarFilaEmpresaAsync(grupo.ToList(), ct));

        var resultadosPorEmpresa = await Task.WhenAll(tarefasPorEmpresa);

        var detalhes = new List<AlertaPendenteDto>();
        var falhasDetalhadas = new List<string>();
        var enviados = 0;
        var falhas = 0;

        foreach (var envio in resultadosPorEmpresa.SelectMany(r => r))
        {
            _db.HistoricoNotificacoes.Add(new HistoricoNotificacao
            {
                IdEmpresa = envio.IdEmpresa,
                IdCliente = envio.Cliente.Id,
                IdRegraAlerta = envio.Regra.Id,
                DataVencimentoReferencia = envio.Cliente.DataVencimento,
                DataEnvio = DateTime.UtcNow,
                Sucesso = envio.Sucesso,
                ErroMensagem = envio.Erro
            });

            detalhes.Add(new AlertaPendenteDto(envio.Cliente.Id, envio.Regra.Id, envio.IdEmpresa, envio.Cliente.Nome, envio.NomeEmpresa, envio.Cliente.Telefone, envio.Regra.Tipo, envio.Regra.DiasOffset, envio.Cliente.DataVencimento));

            if (envio.Sucesso)
            {
                enviados++;
            }
            else
            {
                falhas++;
                falhasDetalhadas.Add($"{envio.Cliente.Nome} ({envio.NomeEmpresa}): {TruncarErro(envio.Erro)}");
            }
        }

        await _db.SaveChangesAsync(ct);

        if (falhasDetalhadas.Count > 0)
            await NotificarSuporteAsync(falhasDetalhadas, ct);

        return new VarreduraResultadoDto(enviados, falhas, detalhes);
    }

    /// <summary>Avisa o suporte (sessão empresa_7, número pessoal do dev) quando o job diário tem falha de envio.</summary>
    private async Task NotificarSuporteAsync(List<string> falhas, CancellationToken ct)
    {
        const string sessaoSuporte = "empresa_7";

        var suporte = await _db.ConfigEmpresas.FirstOrDefaultAsync(e => e.WahaSession == sessaoSuporte, ct);

        if (suporte is null || string.IsNullOrWhiteSpace(suporte.WhatsappDono))
        {
            _logger.LogWarning("Sessão de suporte ({SessaoSuporte}) não configurada — não foi possível notificar falhas do job.", sessaoSuporte);
            return;
        }

        var mensagem = $"Alerty: {falhas.Count} falha(s) ao enviar alertas hoje.\n" + string.Join("\n", falhas);
        var (sucesso, erro) = await _whatsApp.EnviarAsync(suporte.WhatsappDono!, mensagem, sessaoSuporte);

        if (!sucesso)
            _logger.LogWarning("Falha ao notificar suporte sobre erros do job: {Erro}", erro);
    }

    private static string TruncarErro(string? erro)
    {
        if (string.IsNullOrWhiteSpace(erro))
            return "erro desconhecido";

        return erro.Length > 150 ? erro[..150] + "..." : erro;
    }

    private record EnvioRealizado(int IdEmpresa, string? NomeEmpresa, Cliente Cliente, RegraAlerta Regra, bool Sucesso, string? Erro);

    /// <summary>Fila sequencial de UMA empresa: envia, espera um intervalo aleatório, envia o próximo — inclusive antes do relatório do dono. Não grava no banco (isso fica a cargo de quem chama, depois que todas as filas terminarem).</summary>
    private async Task<List<EnvioRealizado>> ProcessarFilaEmpresaAsync(List<AlertaCandidato> candidatos, CancellationToken ct)
    {
        var empresa = candidatos[0].Empresa;
        var resultados = new List<EnvioRealizado>();
        var linhasRelatorio = new List<string>();

        for (var i = 0; i < candidatos.Count; i++)
        {
            if (i > 0)
                await Task.Delay(IntervaloAleatorio(), ct);

            var (cliente, regra) = (candidatos[i].Cliente, candidatos[i].Regra);
            var mensagem = regra.Mensagem.Replace("{nome}", cliente.Nome ?? "");
            var (sucesso, erro) = await _whatsApp.EnviarAsync(cliente.Telefone ?? "", mensagem, empresa.WahaSession);

            resultados.Add(new EnvioRealizado(empresa.IdEmpresa!.Value, empresa.Nome, cliente, regra, sucesso, erro));

            if (sucesso)
                linhasRelatorio.Add(FormatarLinhaRelatorio(cliente.Nome, regra));
        }

        if (linhasRelatorio.Count > 0 && !string.IsNullOrWhiteSpace(empresa.WhatsappDono))
        {
            await Task.Delay(IntervaloAleatorio(), ct);
            var relatorio = string.Join("\n", linhasRelatorio);
            await _whatsApp.EnviarAsync(empresa.WhatsappDono!, relatorio, empresa.WahaSession);
        }

        return resultados;
    }

    private static TimeSpan IntervaloAleatorio() => TimeSpan.FromSeconds(Random.Shared.Next(30, 61));

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
