using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MercadoBonsai.Web.Services;

public class VendedorTokenService
{
    private static readonly byte[] SecretKey = Encoding.UTF8.GetBytes("MercadoBonsaiKey_2026_CartaoVisitas_SecretToken_9988776655443322"); // 32 bytes AES-256
    private static readonly byte[] SecretIv = Encoding.UTF8.GetBytes("MB_Cartao_IV_1234"); // 16 bytes IV

    public string GerarToken(int vendedorId, string secao)
    {
        var rawData = $"{vendedorId}|{secao}|{DateTime.UtcNow.Ticks}";
        
        using var aes = Aes.Create();
        aes.Key = SecretKey;
        aes.IV = SecretIv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var writer = new StreamWriter(cs))
        {
            writer.Write(rawData);
        }

        var cipherBytes = ms.ToArray();
        return Convert.ToHexString(cipherBytes);
    }

    public bool TentarDecodificarToken(string tokenHex, out int vendedorId, out string secao)
    {
        vendedorId = 0;
        secao = string.Empty;

        if (string.IsNullOrWhiteSpace(tokenHex))
            return false;

        try
        {
            var cipherBytes = Convert.FromHexString(tokenHex);

            using var aes = Aes.Create();
            aes.Key = SecretKey;
            aes.IV = SecretIv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs);

            var decrypted = reader.ReadToEnd();
            var parts = decrypted.Split('|');
            if (parts.Length >= 2 && int.TryParse(parts[0], out vendedorId))
            {
                secao = parts[1];
                return true;
            }
        }
        catch
        {
            // Token inválido ou alterado
        }

        return false;
    }
}
