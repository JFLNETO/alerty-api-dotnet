using Microsoft.EntityFrameworkCore;

public class AlertaBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AlertaBackgroundService> _logger;

    public AlertaBackgroundService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<AlertaBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var agora = ObterHorarioAtual();
            var espera = ProximaExecucao(agora) - agora;

            try
            {
                await Task.Delay(espera, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                await ExecutarVarreduraAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao executar a varredura diária de alertas.");
            }
        }
    }

    private async Task ExecutarVarreduraAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var whatsApp = scope.ServiceProvider.GetRequiredService<WhatsAppService>();

        var hoje = DateOnly.FromDateTime(ObterHorarioAtual());
        var empresas = await db.ConfigEmpresas.ToListAsync(ct);

        foreach (var empresa in empresas)
        {
            if (empresa.IdEmpresa is not int idEmpresa)
                continue;

            var regras = await db.RegrasAlerta
                .Where(r => r.IdEmpresa == idEmpresa && r.Ativo)
                .ToListAsync(ct);

            if (regras.Count == 0)
                continue;

            if (string.IsNullOrWhiteSpace(empresa.WahaSession))
            {
                _logger.LogWarning("Empresa {IdEmpresa} tem regras ativas mas nenhuma sessão WAHA configurada — pulando.", idEmpresa);
                continue;
            }

            var clientes = await db.Clientes
                .Where(c => c.IdEmpresa == idEmpresa && c.Ativo)
                .ToListAsync(ct);

            var linhasRelatorio = new List<string>();

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
                    var jaEnviado = await db.HistoricoNotificacoes.AnyAsync(h =>
                        h.IdCliente == cliente.Id &&
                        h.IdRegraAlerta == regra.Id &&
                        h.DataVencimentoReferencia == cliente.DataVencimento &&
                        h.Sucesso, ct);

                    if (jaEnviado)
                        continue;

                    var mensagem = regra.Mensagem.Replace("{nome}", cliente.Nome ?? "");
                    var (sucesso, erro) = await whatsApp.EnviarAsync(cliente.Telefone ?? "", mensagem, empresa.WahaSession);

                    db.HistoricoNotificacoes.Add(new HistoricoNotificacao
                    {
                        IdEmpresa = idEmpresa,
                        IdCliente = cliente.Id,
                        IdRegraAlerta = regra.Id,
                        DataVencimentoReferencia = cliente.DataVencimento,
                        DataEnvio = DateTime.UtcNow,
                        Sucesso = sucesso,
                        ErroMensagem = erro
                    });

                    if (sucesso)
                        linhasRelatorio.Add(FormatarLinhaRelatorio(cliente.Nome, regra));
                }
            }

            await db.SaveChangesAsync(ct);

            if (linhasRelatorio.Count > 0 && !string.IsNullOrWhiteSpace(empresa.WhatsappDono))
            {
                var relatorio = string.Join("\n", linhasRelatorio);
                await whatsApp.EnviarAsync(empresa.WhatsappDono!, relatorio, empresa.WahaSession);
            }
        }
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

    private DateTime ObterHorarioAtual() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ObterTimeZone());

    private DateTime ProximaExecucao(DateTime agora)
    {
        var hora = _config.GetValue<int?>("Alertas:HoraExecucao") ?? 17;
        var hoje = agora.Date.AddHours(hora);
        return agora < hoje ? hoje : hoje.AddDays(1);
    }
}
