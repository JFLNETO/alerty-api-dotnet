using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Limites de requisição por rota sensível. Sem isso, dá pra testar senhas em massa no /auth/login,
/// criar milhares de empresas pelo /auth/registro (o cadastro é aberto) e derrubar o WAHA compartilhado
/// abrindo sessões em sequência.
/// </summary>
public static class RateLimitConfig
{
    public const string Login = "login";
    public const string Registro = "registro";
    public const string Token = "token";
    public const string WhatsApp = "whatsapp";

    public static IServiceCollection AddRateLimitAlerty(this IServiceCollection services)
    {
        // A API roda atrás do proxy da Railway: sem isso, RemoteIpAddress é sempre o IP do proxy e todas as
        // academias cairiam no mesmo balde de limite. ForwardLimit = 1 usa só o último IP do X-Forwarded-For —
        // o que o proxy da Railway anotou —, então um atacante não consegue falsificar o IP mandando o header.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Mesmo formato { erro } do UseExceptionHandler, pra o frontend mostrar a mensagem normalmente.
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { erro = "Muitas tentativas em pouco tempo. Aguarde alguns minutos e tente novamente." }, ct);
            };

            options.AddPolicy(Login, context => PorIp(context, limite: 10, janela: TimeSpan.FromMinutes(5)));
            options.AddPolicy(Registro, context => PorIp(context, limite: 5, janela: TimeSpan.FromHours(1)));
            options.AddPolicy(Token, context => PorIp(context, limite: 30, janela: TimeSpan.FromMinutes(1)));

            // A tela de conexão do WhatsApp consulta o status a cada 3s (20/min) — 60/min dá folga pra ela
            // e ainda impede um script de martelar o WAHA. Particiona por empresa, não por IP: o abuso aqui
            // vem de uma conta logada.
            options.AddPolicy(WhatsApp, context => RateLimitPartition.GetFixedWindowLimiter(
                ChavePorEmpresa(context),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }

    public static string ChavePorIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido";

    public static string ChavePorEmpresa(HttpContext context)
    {
        var idEmpresa = context.User.FindFirst("id_empresa")?.Value;
        return idEmpresa is not null ? $"empresa:{idEmpresa}" : $"ip:{ChavePorIp(context)}";
    }

    private static RateLimitPartition<string> PorIp(HttpContext context, int limite, TimeSpan janela) =>
        RateLimitPartition.GetFixedWindowLimiter(
            ChavePorIp(context),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = limite, Window = janela });
}
