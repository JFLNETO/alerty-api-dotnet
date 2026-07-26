using System.Security.Claims;

public static class RegraAlertaEndpoints
{
    public static void MapRegraAlertaEndpoints(this WebApplication app)
    {
        app.MapGet("/alertas", (ClaimsPrincipal user, RegraAlertaService service) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            return Results.Ok(service.Listar(idEmpresa));
        }).RequireAuthorization();

        app.MapPost("/alertas", (
            ClaimsPrincipal user,
            CriarRegraAlertaRequest request,
            RegraAlertaService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var regra = service.Criar(request, idEmpresa);
            return Results.Created($"/alertas/{regra.Id}", regra);
        }).RequireAuthorization();

        app.MapPut("/alertas/{id}", (
            ClaimsPrincipal user,
            int id,
            AtualizarRegraAlertaRequest request,
            RegraAlertaService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var regra = service.Atualizar(id, request, idEmpresa);
            return Results.Ok(regra);
        }).RequireAuthorization();

        app.MapPatch("/alertas/{id}/status", (ClaimsPrincipal user, int id, bool ativo, RegraAlertaService service) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var regra = service.AlterarStatus(id, ativo, idEmpresa);
            return Results.Ok(regra);
        }).RequireAuthorization();

        app.MapDelete("/alertas/{id}", (ClaimsPrincipal user, int id, RegraAlertaService service) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            service.Deletar(id, idEmpresa);
            return Results.NoContent();
        }).RequireAuthorization();
    }
}
