public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
           app.MapPost("/auth/registro", (RegistrarRequest request, AuthService service) =>
        {
            var resultado = service.Registrar(request);
            return Results.Created("/auth/registro", resultado);
        });

        app.MapPost("/auth/login", (LoginRequest request, AuthService service) =>
        {
            var resultado = service.Login(request.Email, request.Senha, request.ManterConectado);
            return Results.Ok(resultado);
        });

        app.MapPost("/auth/refresh", (TokenRequest request, AuthService service) =>
        {
            var resultado = service.Refresh(request.RefreshToken);
            return Results.Ok(resultado);
        });

        app.MapPost("/auth/logout", (TokenRequest request, AuthService service) =>
        {
            service.Logout(request.RefreshToken);
            return Results.Ok(new { mensagem = "Logout realizado com sucesso." });
        });
    }
}
