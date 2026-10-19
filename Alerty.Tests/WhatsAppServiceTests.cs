using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

public class WhatsAppServiceTests
{
    /// <summary>Captura a URL de cada request em vez de chamar o WAHA de verdade. Por padrão responde 404.</summary>
    private class HandlerFalso : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public HandlerFalso(Func<HttpRequestMessage, HttpResponseMessage>? responder = null) =>
            _responder = responder ?? (_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        public List<Uri> Requisicoes { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requisicoes.Add(request.RequestUri!);
            return Task.FromResult(_responder(request));
        }
    }

    /// <summary>Simula o WAHA: check-exists e sendText respondem com o que cada teste mandar.</summary>
    private static Func<HttpRequestMessage, HttpResponseMessage> Waha(HttpResponseMessage checkExists, HttpResponseMessage sendText) =>
        request => request.RequestUri!.AbsolutePath.Contains("check-exists") ? checkExists : sendText;

    private static HttpResponseMessage Json(HttpStatusCode status, string corpo) =>
        new(status) { Content = new StringContent(corpo, System.Text.Encoding.UTF8, "application/json") };

    private static (WhatsAppService Service, HandlerFalso Handler) CriarService(Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
    {
        var handler = new HandlerFalso(responder);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Waha:BaseUrl"] = "http://waha.local" })
            .Build();

        var service = new WhatsAppService(new HttpClient(handler), config, NullLogger<WhatsAppService>.Instance);
        return (service, handler);
    }

    [Fact]
    public async Task ObterStatusAsync_DeveEscaparNomeDaSessao_QuandoSessaoTemCaracteresDeUrl()
    {
        // Arrange
        var (service, handler) = CriarService();

        // Act
        await service.ObterStatusAsync("empresa_1/stop?x=");

        // Assert — a sessão inteira fica num único segmento, sem virar outro caminho/query do WAHA
        var uri = handler.Requisicoes.Single();
        Assert.Equal("/api/sessions/empresa_1%2Fstop%3Fx%3D", uri.AbsolutePath);
        Assert.Equal("", uri.Query);
    }

    [Fact]
    public async Task ObterStatusAsync_DeveManterUrlOriginal_QuandoSessaoEhNomeNormal()
    {
        // Arrange
        var (service, handler) = CriarService();

        // Act
        await service.ObterStatusAsync("empresa_12");

        // Assert
        Assert.Equal("http://waha.local/api/sessions/empresa_12", handler.Requisicoes.Single().ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("98815689")]
    [InlineData("79988156894123")]
    public async Task EnviarAsync_DeveRetornarTelefoneInvalidoSemChamarWaha_QuandoTelefoneNaoTemDddMaisNumero(string telefone)
    {
        // Arrange
        var (service, handler) = CriarService();

        // Act
        var resultado = await service.EnviarAsync(telefone, "oi", "empresa_1");

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Equal(MotivoFalhaEnvio.TelefoneInvalido, resultado.Motivo);
        Assert.Empty(handler.Requisicoes);
    }

    [Fact]
    public async Task EnviarAsync_DeveRetornarNumeroSemWhatsApp_QuandoCheckExistsDizQueNaoExiste()
    {
        // Arrange
        var (service, handler) = CriarService(Waha(
            Json(HttpStatusCode.OK, """{"numberExists": false, "chatId": null}"""),
            new HttpResponseMessage(HttpStatusCode.Created)));

        // Act
        var resultado = await service.EnviarAsync("79988156894", "oi", "empresa_1");

        // Assert — nem tenta o sendText
        Assert.Equal(MotivoFalhaEnvio.NumeroSemWhatsApp, resultado.Motivo);
        Assert.Single(handler.Requisicoes);
    }

    [Fact]
    public async Task EnviarAsync_DeveRetornarNumeroSemWhatsApp_QuandoSendTextFalhaComNoLidFound()
    {
        // Arrange — check-exists fora do ar, o envio segue e o WAHA reclama do LID
        var (service, _) = CriarService(Waha(
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            Json(HttpStatusCode.InternalServerError, """{"message": "no LID found for 5579988156894@s.whatsapp.net from server"}""")));

        // Act
        var resultado = await service.EnviarAsync("79988156894", "oi", "empresa_1");

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Equal(MotivoFalhaEnvio.NumeroSemWhatsApp, resultado.Motivo);
    }

    [Fact]
    public async Task EnviarAsync_DeveRetornarOutro_QuandoWahaFalhaPorOutroMotivo()
    {
        // Arrange
        var (service, _) = CriarService(Waha(
            Json(HttpStatusCode.OK, """{"numberExists": true, "chatId": "5579988156894@c.us"}"""),
            Json(HttpStatusCode.UnprocessableEntity, """{"message": "session is not ready"}""")));

        // Act
        var resultado = await service.EnviarAsync("79988156894", "oi", "empresa_1");

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Equal(MotivoFalhaEnvio.Outro, resultado.Motivo);
    }

    [Fact]
    public async Task EnviarAsync_DeveRetornarSucessoSemMotivo_QuandoWahaAceitaOEnvio()
    {
        // Arrange
        var (service, _) = CriarService(Waha(
            Json(HttpStatusCode.OK, """{"numberExists": true, "chatId": "5579988156894@c.us"}"""),
            new HttpResponseMessage(HttpStatusCode.Created)));

        // Act
        var resultado = await service.EnviarAsync("(79) 98815-6894", "oi", "empresa_1");

        // Assert
        Assert.True(resultado.Sucesso);
        Assert.Equal(MotivoFalhaEnvio.Nenhum, resultado.Motivo);
    }
}
