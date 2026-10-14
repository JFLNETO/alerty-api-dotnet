using System.Security.Claims;

public static class TurmaEndpoints
{
    public static void MapTurmaEndpoints(this WebApplication app)
    {
        app.MapGet("/turmas", (ClaimsPrincipal user, TurmaService service) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            return Results.Ok(service.Listar(idEmpresa));
        }).RequireAuthorization();

        app.MapPost("/turmas", (
            ClaimsPrincipal user,
            CriarTurmaRequest request,
            TurmaService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var turma = service.Criar(request, idEmpresa);
            return Results.Created($"/turmas/{turma.Id}", turma);
        }).RequireAuthorization();

        app.MapPut("/turmas/{id}", (
            ClaimsPrincipal user,
            int id,
            AtualizarTurmaRequest request,
            TurmaService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var turma = service.Atualizar(id, request, idEmpresa);
            return Results.Ok(turma);
        }).RequireAuthorization();

        app.MapDelete("/turmas/{id}", (ClaimsPrincipal user, int id, TurmaService service) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            service.Deletar(id, idEmpresa);
            return Results.NoContent();
        }).RequireAuthorization();
    }
}
