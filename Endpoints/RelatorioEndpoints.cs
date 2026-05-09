public static class RelatorioEndpoints
{
    public static void MapRelatorioEndpoints(this WebApplication app)
    {
        app.MapGet("/relatorios/pagamentos", (
            int? idEmpresa,
            DateOnly? dataInicio,
            DateOnly? dataFim,
            AppDbContext db) =>
        {
            var query = db.HistoricoPagamentos.AsQueryable();

            if (idEmpresa.HasValue)
                query = query.Where(p => p.IdEmpresa == idEmpresa.Value);

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
        });
    }
}