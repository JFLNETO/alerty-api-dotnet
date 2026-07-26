using System.Security.Claims;

public static class EmpresaEndpoints
{
    public static void MapEmpresaEndpoints(this WebApplication app)
    {
        app.MapGet("/empresa", (ClaimsPrincipal user, ConfigEmpresaService service) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            return Results.Ok(service.Obter(idEmpresa));
        }).RequireAuthorization();

        app.MapPatch("/empresa/waha-sessao", (
            ClaimsPrincipal user,
            AtualizarWahaSessaoRequest request,
            ConfigEmpresaService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var empresa = service.AtualizarWahaSessao(idEmpresa, request.Session);
            return Results.Ok(empresa);
        }).RequireAuthorization();
    }
}
