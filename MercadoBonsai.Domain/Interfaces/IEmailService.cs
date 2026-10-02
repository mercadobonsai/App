using System.Threading.Tasks;

namespace MercadoBonsai.Domain.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Envia e-mail automático contendo a cópia digital completa dos Termos de Uso e Política de Privacidade.
    /// Remetente Oficial: comercial@mercadobonsai.com.br
    /// </summary>
    Task EnviarTermosEPrivacidadeAsync(string destinoEmail, string usuarioNome);
}
