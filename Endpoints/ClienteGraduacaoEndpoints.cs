using System.Security.Claims;

public static class ClienteGraduacaoEndpoints
{
    public static void MapClienteGraduacaoEndpoints(this WebApplication app)
    {
        app.MapGet("/clientes/{id}/graduacoes", (
            ClaimsPrincipal user,
            int id,
            ClienteGraduacaoService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            return Results.Ok(service.ListarPorCliente(id, idEmpresa));
        }).RequireAuthorization();

        app.MapPut("/clientes/{id}/graduacoes/{idServico}", (
            ClaimsPrincipal user,
            int id,
            int idServico,
            DefinirGraduacaoAtualRequest request,
            ClienteGraduacaoService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var estado = service.DefinirGraduacaoAtual(id, idServico, request, idEmpresa);
            return Results.Ok(estado);
        }).RequireAuthorization();

        app.MapPost("/clientes/{id}/graduar", (
            ClaimsPrincipal user,
            int id,
            GraduarClienteRequest request,
            ClienteGraduacaoService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var idUsuario = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var status = service.Graduar(id, request, idUsuario, idEmpresa);
            return Results.Ok(status);
        }).RequireAuthorization();
    }
}
