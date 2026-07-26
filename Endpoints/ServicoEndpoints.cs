using System.Security.Claims;

public static class ServicoEndpoints
{
    public static void MapServicoEndpoints(this WebApplication app)
    {
        app.MapGet("/servicos", (ClaimsPrincipal user, ServicoService service) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            return Results.Ok(service.Listar(idEmpresa));
        }).RequireAuthorization();

        app.MapPost("/servicos", (
            ClaimsPrincipal user,
            CriarServicoRequest request,
            ServicoService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var servico = service.Criar(request, idEmpresa);
            return Results.Created($"/servicos/{servico.Id}", servico);
        }).RequireAuthorization();

        app.MapPut("/servicos/{id}", (
            ClaimsPrincipal user,
            int id,
            AtualizarServicoRequest request,
            ServicoService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var servico = service.Atualizar(id, request, idEmpresa);
            return Results.Ok(servico);
        }).RequireAuthorization();

        app.MapDelete("/servicos/{id}", (ClaimsPrincipal user, int id, ServicoService service) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            service.Deletar(id, idEmpresa);
            return Results.NoContent();
        }).RequireAuthorization();
    }
}
