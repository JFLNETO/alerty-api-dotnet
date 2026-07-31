public static class AdminAlertaEndpoints
{
    public static void MapAdminAlertaEndpoints(this WebApplication app)
    {
        // Lista quem receberia mensagem hoje, sem enviar nada nem gravar histórico.
        app.MapPost("/admin/alertas/simular", async (AlertaJobService job, CancellationToken ct) =>
        {
            var pendentes = await job.SimularAsync(ct);
            return Results.Ok(new { total = pendentes.Count, pendentes });
        }).RequireAuthorization("Admin");

        // Executa a varredura de verdade: envia as mensagens via WAHA e grava o histórico.
        app.MapPost("/admin/alertas/executar", async (AlertaJobService job, CancellationToken ct) =>
        {
            var resultado = await job.ExecutarAsync(ct);
            return Results.Ok(resultado);
        }).RequireAuthorization("Admin");

        // Envia o alerta imediatamente para um único cliente (botão "Enviar teste" do painel).
        app.MapPost("/admin/alertas/enviar-teste", async (EnviarAlertaTesteRequest request, AlertaJobService job, CancellationToken ct) =>
        {
            var (sucesso, erro) = await job.EnviarTesteAsync(request.IdCliente, request.IdRegraAlerta, ct);
            return Results.Ok(new { sucesso, erro });
        }).RequireAuthorization("Admin");
    }
}
