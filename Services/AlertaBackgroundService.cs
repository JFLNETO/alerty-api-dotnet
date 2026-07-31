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
                using var scope = _scopeFactory.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<AlertaJobService>();
                await job.ExecutarAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao executar a varredura diária de alertas.");
            }
        }
    }

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
