using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

public class WahaSessionStatus
{
    public string? Name { get; set; }
    public string? Status { get; set; }
    public WahaSessionMe? Me { get; set; }
}

public class WahaSessionMe
{
    public string? Id { get; set; }
    public string? PushName { get; set; }
}

public class WahaQrCode
{
    public string? Mimetype { get; set; }
    public string? Data { get; set; }
}

public class WahaPairingCode
{
    public string? Code { get; set; }
}

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

        var chatId = $"{TelefoneUtils.NormalizarParaWhatsApp(telefone)}@s.whatsapp.net";

        try
        {
            using var request = ConstruirRequest(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/sendText");
            request.Content = JsonContent.Create(new { session, chatId, text = mensagem });

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

    /// <summary>Cria (e inicia automaticamente) a sessão no WAHA — WAHA por padrão já começa o processo de autenticação.</summary>
    public async Task<WahaSessionStatus> IniciarSessaoAsync(string session)
    {
        var baseUrl = ExigirBaseUrl();

        using var request = ConstruirRequest(HttpMethod.Post, $"{baseUrl}/api/sessions");
        request.Content = JsonContent.Create(new { name = session });

        var resposta = await _http.SendAsync(request);

        // A sessão já existe (ex: tentativa anterior de conexão) — o WAHA não deixa criar de novo,
        // só reiniciar. /start é idempotente: só efetivamente inicia se não estiver rodando.
        if (resposta.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            using var startRequest = ConstruirRequest(HttpMethod.Post, $"{baseUrl}/api/sessions/{session}/start");
            resposta = await _http.SendAsync(startRequest);
        }

        await GarantirSucessoAsync(resposta, "iniciar a sessão");

        return await resposta.Content.ReadFromJsonAsync<WahaSessionStatus>()
            ?? throw new AppException("WAHA retornou resposta vazia ao iniciar a sessão.");
    }

    public async Task<WahaSessionStatus> ObterStatusAsync(string session)
    {
        var baseUrl = ExigirBaseUrl();

        using var request = ConstruirRequest(HttpMethod.Get, $"{baseUrl}/api/sessions/{session}");
        var resposta = await _http.SendAsync(request);

        // Sessão ainda não foi criada no WAHA (nunca chamou /iniciar) — não é um erro, é o estado inicial.
        if (resposta.StatusCode == HttpStatusCode.NotFound)
            return new WahaSessionStatus { Name = session, Status = "NOT_FOUND" };

        await GarantirSucessoAsync(resposta, "consultar o status da sessão");

        return await resposta.Content.ReadFromJsonAsync<WahaSessionStatus>()
            ?? throw new AppException("WAHA retornou resposta vazia ao consultar o status.");
    }

    /// <summary>QR code em base64, pronto pra virar um data URI no <img src>.</summary>
    public async Task<WahaQrCode> ObterQrCodeAsync(string session)
    {
        var baseUrl = ExigirBaseUrl();

        using var request = ConstruirRequest(HttpMethod.Get, $"{baseUrl}/api/{session}/auth/qr");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var resposta = await _http.SendAsync(request);
        await GarantirSucessoAsync(resposta, "gerar o QR code");

        return await resposta.Content.ReadFromJsonAsync<WahaQrCode>()
            ?? throw new AppException("WAHA retornou resposta vazia ao gerar o QR code.");
    }

    public async Task<string> SolicitarPairingCodeAsync(string session, string telefone)
    {
        var baseUrl = ExigirBaseUrl();

        using var request = ConstruirRequest(HttpMethod.Post, $"{baseUrl}/api/{session}/auth/request-code");
        request.Content = JsonContent.Create(new { phoneNumber = telefone });

        var resposta = await _http.SendAsync(request);
        await GarantirSucessoAsync(resposta, "solicitar o pairing code");

        var corpo = await resposta.Content.ReadFromJsonAsync<WahaPairingCode>();
        return corpo?.Code ?? throw new AppException("WAHA retornou resposta vazia ao gerar o pairing code.");
    }

    private string ExigirBaseUrl()
    {
        var baseUrl = _config["Waha:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new AppException("WAHA não configurado (Waha:BaseUrl vazio).");

        return baseUrl.TrimEnd('/');
    }

    private HttpRequestMessage ConstruirRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);

        var apiKey = _config["Waha:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Add("X-Api-Key", apiKey);

        return request;
    }

    private static async Task GarantirSucessoAsync(HttpResponseMessage resposta, string acao)
    {
        if (resposta.IsSuccessStatusCode)
            return;

        var corpo = await resposta.Content.ReadAsStringAsync();
        throw new AppException($"Falha ao {acao} no WAHA ({(int)resposta.StatusCode}): {corpo}");
    }
}
