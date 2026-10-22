using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PagamentoService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<S3UploadService>();
builder.Services.AddScoped<RegraAlertaService>();
builder.Services.AddScoped<ConfigEmpresaService>();
builder.Services.AddScoped<ServicoService>();
builder.Services.AddScoped<TurmaService>();
builder.Services.AddScoped<PresencaService>();
builder.Services.AddScoped<GraduacaoService>();
builder.Services.AddScoped<ClienteGraduacaoService>();
builder.Services.AddHttpClient<WhatsAppService>();
builder.Services.AddScoped<AlertaJobService>();
builder.Services.AddHostedService<AlertaBackgroundService>();

var jwtSecret = JwtConfig.ObterSecretValidado(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireClaim("is_admin", "true"));
});

builder.Services.AddRateLimitAlerty();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://alerty-frontend.vercel.app",
                "https://checkygo.com.br",
                "https://www.checkygo.com.br",
                "https://app.checkygo.com.br"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Primeiro de tudo: corrige o IP do cliente (atrás do proxy da Railway) antes de qualquer coisa usá-lo.
app.UseForwardedHeaders();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (error is AppException appEx)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { erro = appEx.Message });
            return;
        }

        context.RequestServices.GetRequiredService<ILogger<Program>>()
            .LogError(error, "Erro não tratado em {Path}", context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { erro = "Ocorreu um erro interno. Tente novamente mais tarde." });
    });
});

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
// Depois da autenticação: a política de WhatsApp particiona pelo id_empresa do JWT.
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ENDPOINTS
app.MapAuthEndpoints();
app.MapClienteEndpoints();
app.MapPagamentoEndpoints();
app.MapRelatorioEndpoints();
app.MapServicoEndpoints();
app.MapUploadEndpoints();
app.MapRegraAlertaEndpoints();
app.MapEmpresaEndpoints();
app.MapAdminAlertaEndpoints();
app.MapTurmaEndpoints();
app.MapPresencaEndpoints();
app.MapGraduacaoEndpoints();
app.MapClienteGraduacaoEndpoints();

app.Run();
