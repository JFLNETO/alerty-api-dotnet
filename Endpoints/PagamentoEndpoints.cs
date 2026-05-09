public static class PagamentoEndpoints
{
    public static void MapPagamentoEndpoints(this WebApplication app)
    {
        app.MapPost("/pagamentos/confirmar",
            (ConfirmarPagamentoRequest request, PagamentoService service) =>
        {
            try
            {
                var resultado = service.ConfirmarPagamento(request);

                return Results.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
    }
}