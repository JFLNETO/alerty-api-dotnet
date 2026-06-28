public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
           app.MapPost("/auth/registro", (RegistrarRequest request, AuthService service) =>
        {
            try
            {
                var resultado = service.Registrar(request);
                return Results.Created("/auth/registro", resultado);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapPost("/auth/login", (LoginRequest request, AuthService service) =>
        {
            try
            {
                var resultado = service.Login(request.Email, request.Senha, request.ManterConectado);
                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapPost("/auth/refresh", (TokenRequest request, AuthService service) =>
        {
            try
            {
                var resultado = service.Refresh(request.RefreshToken);
                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapPost("/auth/logout", (TokenRequest request, AuthService service) =>
        {
            service.Logout(request.RefreshToken);
            return Results.Ok(new { mensagem = "Logout realizado com sucesso." });
        });
    }
}
