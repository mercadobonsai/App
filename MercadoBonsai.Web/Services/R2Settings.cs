namespace MercadoBonsai.Web.Services;

/// <summary>
/// Configurações de integração com o Cloudflare R2 (Object Storage S3 compatível).
/// </summary>
public class R2Settings
{
    public string AccountId { get; set; } = "4e609512a086d6d87affa24891484652";
    public string BucketName { get; set; } = "mercadobonsai";
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;

    public string ServiceUrl => !string.IsNullOrWhiteSpace(AccountId)
        ? $"https://{AccountId}.r2.cloudflarestorage.com"
        : string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(AccountId) &&
                                !string.IsNullOrWhiteSpace(BucketName) &&
                                !string.IsNullOrWhiteSpace(AccessKeyId) &&
                                !string.IsNullOrWhiteSpace(SecretAccessKey) &&
                                !string.IsNullOrWhiteSpace(PublicUrl);
}
