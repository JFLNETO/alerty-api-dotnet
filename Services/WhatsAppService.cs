using System.Net.Http.Json;

public class WhatsAppService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(HttpClient http, IConfiguration config, ILogger<WhatsAppService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<(bool Sucesso, string? Erro)> EnviarAsync(string telefone, string mensagem, string? session)
    {
        var baseUrl = _config["Waha:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("WAHA não configurado (Waha:BaseUrl vazio) — envio ignorado.");
            return (false, "WAHA não configurado.");
        }

        if (string.IsNullOrWhiteSpace(session))
        {
            _logger.LogWarning("Empresa sem sessão WAHA configurada (ConfigEmpresa.WahaSession) — envio ignorado.");
            return (false, "Empresa sem sessão WAHA configurada.");
        }

        var chatId = $"55{telefone}@s.whatsapp.net";
        var apiKey = _config["Waha:ApiKey"];

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/sendText")
            {
                Content = JsonContent.Create(new { session, chatId, text = mensagem })
            };

            if (!string.IsNullOrWhiteSpace(apiKey))
                request.Headers.Add("X-Api-Key", apiKey);

            var resposta = await _http.SendAsync(request);

            if (!resposta.IsSuccessStatusCode)
            {
                var corpo = await resposta.Content.ReadAsStringAsync();
                _logger.LogWarning("WAHA retornou {StatusCode} ao enviar para {ChatId}: {Corpo}", (int)resposta.StatusCode, chatId, corpo);
                return (false, $"WAHA retornou {(int)resposta.StatusCode}: {corpo}");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar mensagem via WAHA para {ChatId}", chatId);
            return (false, ex.Message);
        }
    }
}
