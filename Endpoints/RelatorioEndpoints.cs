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

            var idsClientes = pagamentos
                .Select(p => int.TryParse(p.ClienteId, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var nomesPorId = db.Clientes
                .Where(c => c.IdEmpresa == idEmpresa && idsClientes.Contains(c.Id))
                .ToDictionary(c => c.Id, c => c.Nome);

            var pagamentosComNome = pagamentos.Select(p => new
            {
                p.Id,
                p.ClienteId,
                clienteNome = int.TryParse(p.ClienteId, out var id) && nomesPorId.TryGetValue(id, out var nome)
                    ? nome
                    : p.ClienteId,
                p.IdEmpresa,
                p.Valor,
                p.DataPagamento,
                p.DataVencimentoAnterior
            });

            var totalRecebido = pagamentos.Sum(p => p.Valor);

            return Results.Ok(new
            {
                totalRecebido,
                quantidadePagamentos = pagamentos.Count,
                pagamentos = pagamentosComNome
            });
        }).RequireAuthorization();
    }
}
