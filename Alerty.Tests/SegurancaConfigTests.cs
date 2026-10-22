using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

public class SegurancaConfigTests
{
    private static IConfiguration ConfigComSecret(string? secret) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = secret })
            .Build();

    // ---------------------------------------------------------------
    // JWT SECRET
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ObterSecretValidado_DeveLancarExcecao_QuandoSecretNaoConfigurado(string? secret)
    {
        // Arrange
        var config = ConfigComSecret(secret);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => JwtConfig.ObterSecretValidado(config));
    }

    [Fact]
    public void ObterSecretValidado_DeveLancarExcecao_QuandoSecretTemMenosDe32Bytes()
    {
        // Arrange
        var config = ConfigComSecret(new string('a', 31));

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => JwtConfig.ObterSecretValidado(config));
    }

    [Fact]
    public void ObterSecretValidado_DeveRetornarSecret_QuandoSecretTem32BytesOuMais()
    {
        // Arrange
        var secret = new string('a', 32);
        var config = ConfigComSecret(secret);

        // Act
        var resultado = JwtConfig.ObterSecretValidado(config);

        // Assert
        Assert.Equal(secret, resultado);
    }

    // ---------------------------------------------------------------
    // RATE LIMIT — CHAVE DE PARTIÇÃO
    // ---------------------------------------------------------------

    [Fact]
    public void ChavePorEmpresa_DeveUsarIdEmpresaDoJwt_QuandoUsuarioAutenticado()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("id_empresa", "13") }, "teste"));

        // Act
        var chave = RateLimitConfig.ChavePorEmpresa(context);

        // Assert — empresas diferentes atrás do mesmo IP não dividem o limite
        Assert.Equal("empresa:13", chave);
    }

    [Fact]
    public void ChavePorEmpresa_DeveCairNoIp_QuandoUsuarioNaoAutenticado()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

        // Act
        var chave = RateLimitConfig.ChavePorEmpresa(context);

        // Assert
        Assert.Equal("ip:10.0.0.1", chave);
    }
}
