using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

public class S3UploadService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly string _region;

    public S3UploadService(IConfiguration config)
    {
        _bucket = config["Aws:Bucket"]!;
        _region = config["Aws:Region"]!;

        _s3 = new AmazonS3Client(
            config["Aws:AccessKeyId"],
            config["Aws:SecretAccessKey"],
            RegionEndpoint.GetBySystemName(_region));
    }

    public (string UploadUrl, string PublicUrl) GerarUrlUpload(int idEmpresa, string extensao, string contentType)
    {
        var chave = $"clientes/{idEmpresa}/{Guid.NewGuid()}.{extensao}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = chave,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(5),
            ContentType = contentType
        };

        var uploadUrl = _s3.GetPreSignedURL(request);
        var publicUrl = $"https://{_bucket}.s3.{_region}.amazonaws.com/{chave}";

        return (uploadUrl, publicUrl);
    }
}
