using System.Security.Claims;

public static class ClienteEndpoints
{
    public static void MapClienteEndpoints(this WebApplication app)
    {
        app.MapGet("/clientes", (
            ClaimsPrincipal user,
            string? nome,
            bool? ativo,
            ClienteService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var clientes = service.Listar(idEmpresa, nome, ativo);
            return Results.Ok(clientes);
        }).RequireAuthorization();

        app.MapGet("/clientes/{id}", (ClaimsPrincipal user, int id, ClienteService service) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var cliente = service.BuscarPorId(id, idEmpresa);

            if (cliente is null)
                return Results.NotFound("Cliente não encontrado.");

            return Results.Ok(cliente);
        }).RequireAuthorization();

        app.MapPost("/clientes", (
            ClaimsPrincipal user,
            CriarClienteRequest request,
            ClienteService service
        ) =>
        {
            try
            {
                request.IdEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
                var novoCliente = service.Criar(request);

                return Results.Created($"/clientes/{novoCliente.Id}", novoCliente);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        }).RequireAuthorization();

        app.MapPut("/clientes/{id}", (
            ClaimsPrincipal user,
            int id,
            AtualizarClienteRequest request,
            ClienteService service
        ) =>
        {
            try
            {
                var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
                request.IdEmpresa = idEmpresa;
                var cliente = service.Atualizar(id, request, idEmpresa);
                return Results.Ok(cliente);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        }).RequireAuthorization();

        app.MapPatch("/clientes/{id}/status", (ClaimsPrincipal user, int id, bool ativo, ClienteService service) =>
        {
            try
            {
                var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
                var cliente = service.AlterarStatus(id, ativo, idEmpresa);
                return Results.Ok(cliente);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization();
    }
}
