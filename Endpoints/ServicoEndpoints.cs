using System.Security.Claims;

public static class ServicoEndpoints
{
    public static void MapServicoEndpoints(this WebApplication app)
    {
        app.MapGet("/servicos", (ClaimsPrincipal user, AppDbContext db) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);

            var servicos = db.Servicos
                .Where(s => s.IdEmpresa == idEmpresa)
                .OrderBy(s => s.Nome)
                .Select(s => new { s.Id, s.Nome })
                .ToList();

            return Results.Ok(servicos);
        }).RequireAuthorization();
    }
}
