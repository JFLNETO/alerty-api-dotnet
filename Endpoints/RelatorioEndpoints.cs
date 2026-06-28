using System.Security.Claims;

public static class RelatorioEndpoints
{
    public static void MapRelatorioEndpoints(this WebApplication app)
    {
        app.MapGet("/relatorios/pagamentos", (
            ClaimsPrincipal user,
            DateOnly? dataInicio,
            DateOnly? dataFim,
            AppDbContext db
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);

            var query = db.HistoricoPagamentos
                .Where(p => p.IdEmpresa == idEmpresa)
                .AsQueryable();

            if (dataInicio.HasValue)
                query = query.Where(p => DateOnly.FromDateTime(p.DataPagamento) >= dataInicio.Value);

            if (dataFim.HasValue)
                query = query.Where(p => DateOnly.FromDateTime(p.DataPagamento) <= dataFim.Value);

            var pagamentos = query
                .OrderByDescending(p => p.DataPagamento)
                .ToList();

            var totalRecebido = pagamentos.Sum(p => p.Valor);

            return Results.Ok(new
            {
                totalRecebido,
                quantidadePagamentos = pagamentos.Count,
                pagamentos
            });
        }).RequireAuthorization();
    }
}
