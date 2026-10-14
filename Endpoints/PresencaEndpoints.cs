using System.Security.Claims;

public static class PresencaEndpoints
{
    public static void MapPresencaEndpoints(this WebApplication app)
    {
        app.MapGet("/turmas/{id}/chamada", (
            ClaimsPrincipal user,
            int id,
            DateOnly data,
            PresencaService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            return Results.Ok(service.BuscarChamada(id, data, idEmpresa));
        }).RequireAuthorization();

        app.MapPost("/turmas/{id}/chamada", (
            ClaimsPrincipal user,
            int id,
            LancarChamadaRequest request,
            PresencaService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var idUsuario = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var presencas = service.LancarChamada(id, request, idUsuario, idEmpresa);
            return Results.Ok(presencas);
        }).RequireAuthorization();
    }
}
