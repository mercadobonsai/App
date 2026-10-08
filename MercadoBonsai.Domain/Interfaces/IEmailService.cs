using System.Threading.Tasks;

namespace MercadoBonsai.Domain.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Dispara e-mail permitindo passar dinamicamente o e-mail e o nome do remetente (senderEmail e senderName).
    /// </summary>
    Task EnviarEmailAsync(string senderEmail, string senderName, string destinoEmail, string destinoNome, string assunto, string mensagemHtml);

    /// <summary>
    /// Dispara e-mail permitindo passar dinamicamente o e-mail e o nome do remetente (senderEmail e senderName) sem destinoNome.
    /// </summary>
    Task EnviarEmailAsync(string senderEmail, string senderName, string destinoEmail, string assunto, string mensagemHtml);

    /// <summary>
    /// Envia e-mail automático contendo a cópia digital completa dos Termos de Uso e Política de Privacidade.
    /// Remetente Oficial: comercial@mercadobonsai.com.br
    /// </summary>
    Task EnviarTermosEPrivacidadeAsync(string destinoEmail, string usuarioNome);

    /// <summary>
    /// Envia e-mail de recuperação/redefinição de senha com link temporário seguro.
    /// Remetente Oficial: suporte@mercadobonsai.com.br
    /// </summary>
    Task EnviarRecuperacaoSenhaAsync(string destinoEmail, string usuarioNome, string linkRedefinicao);
}

