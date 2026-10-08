namespace MercadoBonsai.Web.Services;

/// <summary>
/// Configurações de conexão e integração com o servidor Postal SMTP / API.
/// </summary>
public class PostalSettings
{
    public string Host { get; set; } = "178.105.206.82";
    public int Port { get; set; } = 25;
    public int Porta { get => Port; set => Port = value; }
    
    /// <summary>
    /// Chave/Token de autenticação do Postal (usada na autenticação SMTP e/ou API).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    private string _username = string.Empty;
    public string Username
    {
        get => !string.IsNullOrWhiteSpace(_username) ? _username : Token;
        set => _username = value;
    }

    private string _password = string.Empty;
    public string Password
    {
        get => !string.IsNullOrWhiteSpace(_password) ? _password : Token;
        set => _password = value;
    }

    /// <summary>
    /// Remetente padrão caso nenhum remetente específico seja informado.
    /// </summary>
    public string SenderEmail { get; set; } = "no-reply@e-vendas.net.br";
    public string SenderName { get; set; } = "E-Vendas Marketing";

    /// <summary>
    /// URL base da API HTTP do Postal.
    /// </summary>
    public string ApiUrl { get; set; } = "https://mail.e-vendas.net.br";
}
