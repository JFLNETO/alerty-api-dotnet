using System.Security.Claims;

public static class PagamentoEndpoints
{
    public static void MapPagamentoEndpoints(this WebApplication app)
    {
        app.MapPost("/pagamentos/confirmar", (
            ClaimsPrincipal user,
            ConfirmarPagamentoRequest request,
            PagamentoService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var resultado = service.ConfirmarPagamento(request, idEmpresa);
            return Results.Ok(resultado);
        }).RequireAuthorization();
    }
}
