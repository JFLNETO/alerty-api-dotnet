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

public class WahaCheckExistsResponse
{
    public bool NumberExists { get; set; }
    public string? ChatId { get; set; }
}

public record ResultadoEnvio(bool Sucesso, string? Erro, MotivoFalhaEnvio Motivo)
{
    public static ResultadoEnvio Ok() => new(true, null, MotivoFalhaEnvio.Nenhum);
    public static ResultadoEnvio Falha(string? erro, MotivoFalhaEnvio motivo = MotivoFalhaEnvio.Outro) => new(false, erro, motivo);
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

    public async Task<ResultadoEnvio> EnviarAsync(string telefone, string mensagem, string? session)
    {
        var baseUrl = _config["Waha:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("WAHA não configurado (Waha:BaseUrl vazio) — envio ignorado.");
            return ResultadoEnvio.Falha("WAHA não configurado.");
        }

        if (string.IsNullOrWhiteSpace(session))
        {
            _logger.LogWarning("Empresa sem sessão WAHA configurada (ConfigEmpresa.WahaSession) — envio ignorado.");
            return ResultadoEnvio.Falha("Empresa sem sessão WAHA configurada.");
        }

        // Telefone canônico é DDD + número (10 ou 11 dígitos, só Brasil — ver TelefoneUtils). Fora disso
        // nem adianta perguntar pro WAHA: é cadastro errado, e o dono da academia é quem consegue corrigir.
        if (TelefoneUtils.RemoverDdi(telefone).Length is not (10 or 11))
        {
            _logger.LogWarning("Telefone {Telefone} fora do formato DDD + número — envio cancelado.", telefone);
            return ResultadoEnvio.Falha("Telefone inválido (esperado DDD + número).", MotivoFalhaEnvio.TelefoneInvalido);
        }

        baseUrl = baseUrl.TrimEnd('/');
        var numero = TelefoneUtils.NormalizarParaWhatsApp(telefone);
        var chatId = $"{numero}@s.whatsapp.net";

        // Checa antes se o numero existe no WhatsApp: evita o 500 confuso do WAHA ("no LID found")
        // pra numeros invalidos, e o proprio WAHA recomenda essa chamada pra popular o mapeamento
        // lid -> telefone antes do envio. Se a checagem falhar, segue pro envio normal mesmo assim —
        // e so uma tentativa de evitar o erro, nao pode virar um novo ponto de falha.
        try
        {
            var checagem = await VerificarNumeroExisteAsync(baseUrl, session, numero);

            if (checagem is not null && !checagem.NumberExists)
            {
                _logger.LogWarning("Número {Numero} não está registrado no WhatsApp — envio cancelado.", numero);
                return ResultadoEnvio.Falha("Número não está registrado no WhatsApp.", MotivoFalhaEnvio.NumeroSemWhatsApp);
            }

            if (checagem is not null && !string.IsNullOrWhiteSpace(checagem.ChatId))
            {
                if (ChatIdCorrespondeAoNumero(checagem.ChatId, numero))
                {
                    chatId = checagem.ChatId;
                }
                else
                {
                    _logger.LogWarning(
                        "chatId devolvido pelo WAHA ({ChatIdDevolvido}) não bate com o número consultado ({Numero}) — ignorando e mantendo o chatId montado localmente.",
                        checagem.ChatId, numero);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao checar existência do número {Numero} no WAHA — seguindo com o envio mesmo assim.", numero);
        }

        try
        {
            using var request = ConstruirRequest(HttpMethod.Post, $"{baseUrl}/api/sendText");
            request.Content = JsonContent.Create(new { session, chatId, text = mensagem });

            var resposta = await _http.SendAsync(request);

            if (!resposta.IsSuccessStatusCode)
            {
                var corpo = await resposta.Content.ReadAsStringAsync();
                _logger.LogWarning("WAHA retornou {StatusCode} ao enviar para {ChatId}: {Corpo}", (int)resposta.StatusCode, chatId, corpo);

                // Se o check-exists falhou e o envio seguiu, número inexistente aparece aqui como "no LID found".
                var motivo = corpo.Contains("no LID found", StringComparison.OrdinalIgnoreCase)
                    ? MotivoFalhaEnvio.NumeroSemWhatsApp
                    : MotivoFalhaEnvio.Outro;

                return ResultadoEnvio.Falha($"WAHA retornou {(int)resposta.StatusCode}: {corpo}", motivo);
            }

            return ResultadoEnvio.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar mensagem via WAHA para {ChatId}", chatId);
            return ResultadoEnvio.Falha(ex.Message);
        }
    }

    /// <summary>Checa se o número existe no WhatsApp — GET /api/contacts/check-exists, conforme docs do WAHA.</summary>
    private async Task<WahaCheckExistsResponse?> VerificarNumeroExisteAsync(string baseUrl, string session, string numero)
    {
        using var request = ConstruirRequest(HttpMethod.Get, $"{baseUrl}/api/contacts/check-exists?phone={Uri.EscapeDataString(numero)}&session={Uri.EscapeDataString(session)}");
        var resposta = await _http.SendAsync(request);

        if (!resposta.IsSuccessStatusCode)
            return null;

        return await resposta.Content.ReadFromJsonAsync<WahaCheckExistsResponse>();
    }

    /// <summary>
    /// Confere se o chatId devolvido pelo check-exists realmente corresponde ao número consultado —
    /// trava de segurança pra nunca mandar mensagem pro destinatário errado por causa de uma resposta
    /// inesperada do WAHA. Formato @lid é um identificador opaco do WhatsApp sem relação numérica com
    /// o telefone (é justamente pra esse caso que o check-exists existe), então nesse formato confiamos
    /// sem comparar dígitos.
    /// </summary>
    private static bool ChatIdCorrespondeAoNumero(string chatIdDevolvido, string numeroEsperado)
    {
        if (chatIdDevolvido.EndsWith("@lid", StringComparison.OrdinalIgnoreCase))
            return true;

        var digitosDevolvidos = TelefoneUtils.ApenasDigitos(chatIdDevolvido);
        var sufixoEsperado = numeroEsperado.Length >= 8 ? numeroEsperado[^8..] : numeroEsperado;

        return digitosDevolvidos.EndsWith(sufixoEsperado);
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
            using var startRequest = ConstruirRequest(HttpMethod.Post, $"{baseUrl}/api/sessions/{Uri.EscapeDataString(session)}/start");
            resposta = await _http.SendAsync(startRequest);
        }

        await GarantirSucessoAsync(resposta, "iniciar a sessão");

        return await resposta.Content.ReadFromJsonAsync<WahaSessionStatus>()
            ?? throw new AppException("WAHA retornou resposta vazia ao iniciar a sessão.");
    }

    public async Task<WahaSessionStatus> ObterStatusAsync(string session)
    {
        var baseUrl = ExigirBaseUrl();

        using var request = ConstruirRequest(HttpMethod.Get, $"{baseUrl}/api/sessions/{Uri.EscapeDataString(session)}");
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

        using var request = ConstruirRequest(HttpMethod.Get, $"{baseUrl}/api/{Uri.EscapeDataString(session)}/auth/qr");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var resposta = await _http.SendAsync(request);
        await GarantirSucessoAsync(resposta, "gerar o QR code");

        return await resposta.Content.ReadFromJsonAsync<WahaQrCode>()
            ?? throw new AppException("WAHA retornou resposta vazia ao gerar o QR code.");
    }

    public async Task<string> SolicitarPairingCodeAsync(string session, string telefone)
    {
        var baseUrl = ExigirBaseUrl();

        using var request = ConstruirRequest(HttpMethod.Post, $"{baseUrl}/api/{Uri.EscapeDataString(session)}/auth/request-code");
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
