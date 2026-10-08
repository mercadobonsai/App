using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MercadoBonsai.Web.Services;

/// <summary>
/// Contrato para serviços de armazenamento de arquivos e imagens.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Otimiza a imagem (redimensiona e converte para WebP) e salva no Cloudflare R2 ou em disco local.
    /// Retorna a URL pública ou caminho relativo da imagem salva.
    /// </summary>
    Task<string> UploadImagemOtimizadaAsync(IFormFile arquivo, string pastaDestino, string? prefixoNome = null);
}
