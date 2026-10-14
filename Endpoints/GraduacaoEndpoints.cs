using System.Security.Claims;

public static class GraduacaoEndpoints
{
    public static void MapGraduacaoEndpoints(this WebApplication app)
    {
        app.MapGet("/servicos/{idServico}/graduacoes", (
            ClaimsPrincipal user,
            int idServico,
            GraduacaoService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            return Results.Ok(service.Listar(idServico, idEmpresa));
        }).RequireAuthorization();

        app.MapPost("/graduacoes", (
            ClaimsPrincipal user,
            CriarGraduacaoRequest request,
            GraduacaoService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var graduacao = service.Criar(request, idEmpresa);
            return Results.Created($"/graduacoes/{graduacao.Id}", graduacao);
        }).RequireAuthorization();

        app.MapPut("/graduacoes/{id}", (
            ClaimsPrincipal user,
            int id,
            AtualizarGraduacaoRequest request,
            GraduacaoService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var graduacao = service.Atualizar(id, request, idEmpresa);
            return Results.Ok(graduacao);
        }).RequireAuthorization();

        app.MapDelete("/graduacoes/{id}", (ClaimsPrincipal user, int id, GraduacaoService service) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            service.Deletar(id, idEmpresa);
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapPost("/graduacoes/modelo-padrao", (
            ClaimsPrincipal user,
            int idServico,
            GraduacaoService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var graduacoes = service.CriarModeloPadraoJiuJitsu(idServico, idEmpresa);
            return Results.Ok(graduacoes);
        }).RequireAuthorization();
    }
}
