public static class ClienteEndpoints
{
    public static void MapClienteEndpoints(this WebApplication app)
    {
        app.MapGet("/clientes",
            (
                int? idEmpresa,
                string? nome,
                bool? ativo,
                ClienteService service
            ) =>
        {
            var clientes = service.Listar(idEmpresa, nome, ativo);

            return Results.Ok(clientes);
        });

        app.MapGet("/clientes/{id}",
            (int id, ClienteService service) =>
        {
            var cliente = service.BuscarPorId(id);

            if (cliente is null)
                return Results.NotFound("Cliente não encontrado.");

            return Results.Ok(cliente);
        });

    app.MapPost("/clientes",
    (CriarClienteRequest request, ClienteService service) =>
{
    try
    {
        var novoCliente = service.Criar(request);

        return Results.Created(
            $"/clientes/{novoCliente.Id}",
            novoCliente
        );
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            erro = ex.Message
        });
    }
});

        app.MapPut("/clientes/{id}",
            (int id, AtualizarClienteRequest request, ClienteService service) =>
        {
            try
            {
                var cliente = service.Atualizar(id, request);

                return Results.Ok(cliente);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        app.MapPatch("/clientes/{id}/status",
            (int id, bool ativo, ClienteService service) =>
        {
            try
            {
                var cliente = service.AlterarStatus(id, ativo);

                return Results.Ok(cliente);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
    }
}