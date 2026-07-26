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

        app.MapPost("/empresa/whatsapp/iniciar", async (
            ClaimsPrincipal user,
            ConfigEmpresaService empresaService,
            WhatsAppService whatsApp
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var session = empresaService.GarantirWahaSession(idEmpresa);
            var status = await whatsApp.IniciarSessaoAsync(session);
            return Results.Ok(status);
        }).RequireAuthorization();

        app.MapGet("/empresa/whatsapp/status", async (
            ClaimsPrincipal user,
            ConfigEmpresaService empresaService,
            WhatsAppService whatsApp
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var session = empresaService.GarantirWahaSession(idEmpresa);
            var status = await whatsApp.ObterStatusAsync(session);
            return Results.Ok(status);
        }).RequireAuthorization();

        app.MapGet("/empresa/whatsapp/qr", async (
            ClaimsPrincipal user,
            ConfigEmpresaService empresaService,
            WhatsAppService whatsApp
        ) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var session = empresaService.GarantirWahaSession(idEmpresa);
            var qr = await whatsApp.ObterQrCodeAsync(session);
            return Results.Ok(qr);
        }).RequireAuthorization();

        app.MapPost("/empresa/whatsapp/pairing-code", async (
            ClaimsPrincipal user,
            SolicitarPairingCodeRequest request,
            ConfigEmpresaService empresaService,
            WhatsAppService whatsApp
        ) =>
        {
            if (string.IsNullOrWhiteSpace(request.Telefone))
                throw new AppException("Informe o telefone com DDI e DDD.");

            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            var session = empresaService.GarantirWahaSession(idEmpresa);
            var codigo = await whatsApp.SolicitarPairingCodeAsync(session, request.Telefone);
            return Results.Ok(new { codigo });
        }).RequireAuthorization();
    }
}
