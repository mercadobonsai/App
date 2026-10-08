using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MercadoBonsai.Web.Services;

/// <summary>
/// Serviço de armazenamento de imagens com otimização WebP automática e upload para Cloudflare R2.
/// Possui fallback transparente para disco local caso as credenciais do R2 não estejam configuradas.
/// </summary>
public class R2StorageService : IStorageService
{
    private readonly R2Settings _settings;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<R2StorageService> _logger;

    public R2StorageService(
        IOptions<R2Settings> r2Options,
        IConfiguration configuration,
        IWebHostEnvironment webHostEnvironment,
        ILogger<R2StorageService> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
        _settings = r2Options?.Value ?? new R2Settings();

        // Fallback de vinculação direta por IConfiguration
        var section = configuration.GetSection("CloudflareR2");
        if (section.Exists())
        {
            if (!string.IsNullOrWhiteSpace(section["AccountId"])) _settings.AccountId = section["AccountId"]!;
            if (!string.IsNullOrWhiteSpace(section["BucketName"])) _settings.BucketName = section["BucketName"]!;
            if (!string.IsNullOrWhiteSpace(section["AccessKeyId"])) _settings.AccessKeyId = section["AccessKeyId"]!;
            if (!string.IsNullOrWhiteSpace(section["SecretAccessKey"])) _settings.SecretAccessKey = section["SecretAccessKey"]!;
            if (!string.IsNullOrWhiteSpace(section["PublicUrl"])) _settings.PublicUrl = section["PublicUrl"]!;
        }
    }

    public async Task<string> UploadImagemOtimizadaAsync(IFormFile arquivo, string pastaDestino, string? prefixoNome = null)
    {
        if (arquivo == null || arquivo.Length == 0)
        {
            return string.Empty;
        }

        string prefixo = !string.IsNullOrWhiteSpace(prefixoNome) ? $"{prefixoNome}_" : string.Empty;
        string nomeArquivo = $"{prefixo}{Guid.NewGuid():N}.webp";
        pastaDestino = pastaDestino.Trim('/', '\\');
        string chaveR2 = string.IsNullOrWhiteSpace(pastaDestino) ? nomeArquivo : $"{pastaDestino}/{nomeArquivo}";

        // 1. Processamento e Otimização em Memória (Redimensionar max 1200px + Converter para WebP)
        using var memoryStreamWebp = new MemoryStream();
        try
        {
            using var inputStream = arquivo.OpenReadStream();
            using var image = await Image.LoadAsync(inputStream);

            int maxDimensao = 1200;
            if (image.Width > maxDimensao || image.Height > maxDimensao)
            {
                var options = new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxDimensao, maxDimensao)
                };
                image.Mutate(x => x.Resize(options));
            }

            var encoder = new WebpEncoder
            {
                Quality = 80,
                FileFormat = WebpFileFormatType.Lossy
            };

            await image.SaveAsWebpAsync(memoryStreamWebp, encoder);
            memoryStreamWebp.Position = 0;

            _logger.LogInformation(
                "[R2StorageService] Imagem '{OriginalName}' otimizada com sucesso. Peso original: {TamanhoOriginal} KB | Peso WebP: {TamanhoWebp} KB.",
                arquivo.FileName, arquivo.Length / 1024, memoryStreamWebp.Length / 1024);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[R2StorageService] Falha ao processar/otimizar imagem. Usando stream original.");
            memoryStreamWebp.SetLength(0);
            await arquivo.CopyToAsync(memoryStreamWebp);
            memoryStreamWebp.Position = 0;
            // Mantém extensão original caso o ImageSharp não tenha conseguido ler
            var extOriginal = Path.GetExtension(arquivo.FileName);
            nomeArquivo = $"{prefixo}{Guid.NewGuid():N}{extOriginal}";
            chaveR2 = string.IsNullOrWhiteSpace(pastaDestino) ? nomeArquivo : $"{pastaDestino}/{nomeArquivo}";
        }

        // 2. Upload para o Cloudflare R2 (se configurado)
        if (_settings.IsConfigured)
        {
            try
            {
                var s3Config = new AmazonS3Config
                {
                    ServiceURL = _settings.ServiceUrl,
                    ForcePathStyle = true
                };

                var credentials = new BasicAWSCredentials(_settings.AccessKeyId, _settings.SecretAccessKey);
                using var client = new AmazonS3Client(credentials, s3Config);

                var putRequest = new PutObjectRequest
                {
                    BucketName = _settings.BucketName,
                    Key = chaveR2,
                    InputStream = memoryStreamWebp,
                    ContentType = "image/webp",
                    DisablePayloadSigning = true
                };

                await client.PutObjectAsync(putRequest);

                string publicUrlBase = _settings.PublicUrl.TrimEnd('/');
                string urlFinal = $"{publicUrlBase}/{chaveR2}";

                _logger.LogInformation("[R2StorageService] Upload concluído no Cloudflare R2: {Url}", urlFinal);
                return urlFinal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[R2StorageService] Falha ao enviar para o Cloudflare R2. Recorrendo ao armazenamento em disco local.");
            }
        }

        // 3. Fallback: Armazenamento em Disco Local (wwwroot/uploads/{pastaDestino})
        var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", pastaDestino);
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var caminhoFisico = Path.Combine(uploadsDir, nomeArquivo);
        memoryStreamWebp.Position = 0;
        using (var fileStream = new FileStream(caminhoFisico, FileMode.Create))
        {
            await memoryStreamWebp.CopyToAsync(fileStream);
        }

        string caminhoRelativo = $"/uploads/{pastaDestino}/{nomeArquivo}";
        _logger.LogInformation("[R2StorageService] Imagem salva localmente no disco: {Caminho}", caminhoRelativo);
        return caminhoRelativo;
    }
}
