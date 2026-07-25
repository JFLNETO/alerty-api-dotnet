using System.Security.Claims;

public static class UploadEndpoints
{
    private static readonly HashSet<string> ExtensoesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "jpg", "jpeg", "png", "webp"
    };

    public static void MapUploadEndpoints(this WebApplication app)
    {
        app.MapPost("/uploads/foto-url", (
            ClaimsPrincipal user,
            UploadFotoUrlRequest request,
            S3UploadService service
        ) =>
        {
            var extensao = request.Extensao?.TrimStart('.').ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(extensao) || !ExtensoesPermitidas.Contains(extensao))
                throw new AppException("Extensão de arquivo não permitida. Use jpg, jpeg, png ou webp.");

            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);

            var (uploadUrl, publicUrl) = service.GerarUrlUpload(
                idEmpresa,
                extensao,
                string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType);

            return Results.Ok(new { uploadUrl, publicUrl });
        }).RequireAuthorization();
    }
}
