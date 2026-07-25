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
                return Results.NotFound(new { erro = "Cliente não encontrado." });

            return Results.Ok(cliente);
        }).RequireAuthorization();

        app.MapPost("/clientes", (
            ClaimsPrincipal user,
            CriarClienteRequest request,
            ClienteService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var novoCliente = service.Criar(request, idEmpresa);

            return Results.Created($"/clientes/{novoCliente.Id}", novoCliente);
        }).RequireAuthorization();

        app.MapPut("/clientes/{id}", (
            ClaimsPrincipal user,
            int id,
            AtualizarClienteRequest request,
            ClienteService service
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var cliente = service.Atualizar(id, request, idEmpresa);
            return Results.Ok(cliente);
        }).RequireAuthorization();

        app.MapPatch("/clientes/{id}/status", (ClaimsPrincipal user, int id, bool ativo, ClienteService service) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var cliente = service.AlterarStatus(id, ativo, idEmpresa);
            return Results.Ok(cliente);
        }).RequireAuthorization();
    }
}
