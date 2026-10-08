using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MercadoBonsai.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MercadoBonsai.Web.Services;

/// <summary>
/// Serviço de envio de e-mails via servidor Postal (SMTP) utilizando MailKit.
/// Suporta credenciais globais (Host, Port, Token/Password) e remetentes dinâmicos a cada chamada.
/// </summary>
public class EmailService : IEmailService
{
    private readonly PostalSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<PostalSettings> postalOptions,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _logger = logger;
        _settings = postalOptions?.Value ?? new PostalSettings();

        // Vincula configurações caso não tenham vindo via IOptions
        var section = configuration.GetSection("PostalSettings").Exists()
            ? configuration.GetSection("PostalSettings")
            : configuration.GetSection("Postal");

        if (section.Exists())
        {
            if (string.IsNullOrWhiteSpace(_settings.Host) || _settings.Host == "178.105.206.82")
            {
                var h = section["Host"] ?? section["Server"];
                if (!string.IsNullOrWhiteSpace(h)) _settings.Host = h;
            }

            var portStr = section["Port"] ?? section["Porta"];
            if (int.TryParse(portStr, out var p) && p > 0)
            {
                _settings.Port = p;
            }

            var token = section["Token"];
            if (!string.IsNullOrWhiteSpace(token)) _settings.Token = token;

            var user = section["Username"] ?? section["Usuario"];
            if (!string.IsNullOrWhiteSpace(user)) _settings.Username = user;

            var pass = section["Password"] ?? section["Senha"];
            if (!string.IsNullOrWhiteSpace(pass)) _settings.Password = pass;

            var senderEmail = section["SenderEmail"];
            if (!string.IsNullOrWhiteSpace(senderEmail)) _settings.SenderEmail = senderEmail;

            var senderName = section["SenderName"];
            if (!string.IsNullOrWhiteSpace(senderName)) _settings.SenderName = senderName;

            var apiUrl = section["ApiUrl"];
            if (!string.IsNullOrWhiteSpace(apiUrl)) _settings.ApiUrl = apiUrl;
        }
    }

    /// <summary>
    /// Dispara e-mail permitindo passar dinamicamente o e-mail e o nome do remetente (senderEmail e senderName).
    /// </summary>
    public async Task EnviarEmailAsync(
        string senderEmail,
        string senderName,
        string destinoEmail,
        string destinoNome,
        string assunto,
        string mensagemHtml)
    {
        if (string.IsNullOrWhiteSpace(destinoEmail))
        {
            _logger.LogWarning("[EmailService] Tentativa de envio com e-mail de destino em branco.");
            return;
        }

        if (string.IsNullOrWhiteSpace(senderEmail))
        {
            senderEmail = !string.IsNullOrWhiteSpace(_settings.SenderEmail) 
                ? _settings.SenderEmail 
                : "no-reply@e-vendas.net.br";
        }

        if (string.IsNullOrWhiteSpace(senderName))
        {
            senderName = !string.IsNullOrWhiteSpace(_settings.SenderName) 
                ? _settings.SenderName 
                : "Mercado Bonsai";
        }

        // Se o Host ou as credenciais não estiverem informadas, registra log informativo
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            _logger.LogWarning("[EmailService] Host do Postal não configurado. Disparo cancelado.");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(string.IsNullOrWhiteSpace(destinoNome) ? destinoEmail : destinoNome, destinoEmail));
            message.Subject = assunto ?? string.Empty;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = mensagemHtml ?? string.Empty
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            client.Timeout = 15000;
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.CheckCertificateRevocation = false;

            SecureSocketOptions socketOptions = _settings.Port switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                25 => SecureSocketOptions.Auto,
                _ => SecureSocketOptions.Auto
            };

            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions);

            var authUser = _settings.Username;
            var authPass = _settings.Password;

            if (!string.IsNullOrWhiteSpace(authUser) && !string.IsNullOrWhiteSpace(authPass))
            {
                await client.AuthenticateAsync(authUser, authPass);
            }

            var response = await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation(
                "[EmailService] E-mail disparado com sucesso para {Destino} via Postal ({Host}:{Port}). Remetente: {SenderName} <{SenderEmail}>. Resposta: {Resposta}",
                destinoEmail, _settings.Host, _settings.Port, senderName, senderEmail, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[EmailService] Falha ao enviar e-mail para {Destino} via Postal ({Host}:{Port}) com remetente {SenderEmail}.",
                destinoEmail, _settings.Host, _settings.Port, senderEmail);
        }
    }

    /// <summary>
    /// Sobrecarga conveniente para disparo sem especificação de nome do destinatário.
    /// </summary>
    public Task EnviarEmailAsync(
        string senderEmail,
        string senderName,
        string destinoEmail,
        string assunto,
        string mensagemHtml)
    {
        return EnviarEmailAsync(senderEmail, senderName, destinoEmail, destinoEmail, assunto, mensagemHtml);
    }

    /// <summary>
    /// Envia e-mail contendo os Termos de Uso e Política de Privacidade.
    /// </summary>
    public async Task EnviarTermosEPrivacidadeAsync(string destinoEmail, string usuarioNome)
    {
        if (string.IsNullOrWhiteSpace(destinoEmail)) return;

        string senderEmail = !string.IsNullOrWhiteSpace(_settings.SenderEmail)
            ? _settings.SenderEmail
            : "comercial@mercadobonsai.com.br";
        string senderName = !string.IsNullOrWhiteSpace(_settings.SenderName)
            ? _settings.SenderName
            : "Mercado Bonsai Comercial";
        string assunto = "Bem-vindo ao Mercado Bonsai - Termos de Uso e Política de Privacidade";

        var bodyBuilder = new StringBuilder();
        bodyBuilder.AppendLine("<!DOCTYPE html>");
        bodyBuilder.AppendLine("<html lang='pt-BR'>");
        bodyBuilder.AppendLine("<head><meta charset='UTF-8'><style>");
        bodyBuilder.AppendLine("body { font-family: Arial, sans-serif; color: #333; line-height: 1.6; max-width: 650px; margin: 0 auto; padding: 20px; }");
        bodyBuilder.AppendLine(".header { background-color: #4A7C59; color: #fff; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }");
        bodyBuilder.AppendLine(".content { border: 1px solid #e0e0e0; padding: 25px; background: #ffffff; border-radius: 0 0 8px 8px; }");
        bodyBuilder.AppendLine(".section-title { color: #4A7C59; border-bottom: 2px solid #4A7C59; padding-bottom: 5px; margin-top: 25px; }");
        bodyBuilder.AppendLine(".footer { text-align: center; margin-top: 25px; font-size: 12px; color: #777; }");
        bodyBuilder.AppendLine("</style></head>");
        bodyBuilder.AppendLine("<body>");
        bodyBuilder.AppendLine("<div class='header'><h1>🌱 Mercado Bonsai</h1><p>Conectando Cultivadores e Apaixonados pela Arte Bonsai</p></div>");
        bodyBuilder.AppendLine("<div class='content'>");
        bodyBuilder.AppendLine($"<p>Olá, <strong>{WebUtility.HtmlEncode(usuarioNome)}</strong>!</p>");
        bodyBuilder.AppendLine("<p>Seja muito bem-vindo ao <strong>Mercado Bonsai</strong>. Em atendimento às normas vigentes e ao nosso compromisso com a transparência e conformidade com a LGPD, enviamos em anexo a esta mensagem a cópia digital completa dos nossos <strong>Termos de Uso</strong> e <strong>Política de Privacidade</strong>.</p>");

        bodyBuilder.AppendLine("<h2 class='section-title'>📄 Resumo dos Termos de Uso</h2>");
        bodyBuilder.AppendLine("<p>Ao se cadastrar como usuário ou vendedor na plataforma Mercado Bonsai, você concorda com:");
        bodyBuilder.AppendLine("<ul>");
        bodyBuilder.AppendLine("<li><strong>Cadastro e Responsabilidade:</strong> Fornecimento de dados verídicos e guarda segura de suas credenciais.</li>");
        bodyBuilder.AppendLine("<li><strong>Regras de Venda e Anúncios:</strong> Manutenção de ofertas legítimas, produtos com estoque físico garantido e fotos autênticas. Vendedores devem manter plano de assinatura ativo para veiculação dos anúncios.</li>");
        bodyBuilder.AppendLine("<li><strong>Repasses e Comissões:</strong> Processamento seguro de pagamentos e repasses via Asaas com retenção das taxas aplicáveis ao plano contratado.</li>");
        bodyBuilder.AppendLine("<li><strong>Compromisso de Entrega:</strong> Cumprimento dos prazos de postagem e fornecimento de código de rastreamento válido.</li>");
        bodyBuilder.AppendLine("</ul></p>");

        bodyBuilder.AppendLine("<h2 class='section-title'>🔒 Resumo da Política de Privacidade (LGPD)</h2>");
        bodyBuilder.AppendLine("<p>Seus dados pessoais são coletados estritamente para viabilizar as transações comerciais, emissão de cobranças e entrega dos produtos. Não compartilhamos suas informações com terceiros para fins publicitários não autorizados.</p>");

        bodyBuilder.AppendLine("<p>Você pode acessar a versão completa a qualquer momento em nosso site no rodapé institucional ou através dos links diretos:</p>");
        bodyBuilder.AppendLine("<p>• <a href='https://mercadobonsai.com.br/Home/Termos' style='color: #4A7C59; font-weight: bold;'>Termos de Uso Integrais</a><br>");
        bodyBuilder.AppendLine("• <a href='https://mercadobonsai.com.br/Home/Privacidade' style='color: #4A7C59; font-weight: bold;'>Política de Privacidade Integral</a></p>");

        bodyBuilder.AppendLine("<p style='margin-top: 30px;'>Atenciosamente,<br><strong>Equipe Mercado Bonsai</strong><br><small>comercial@mercadobonsai.com.br</small></p>");
        bodyBuilder.AppendLine("</div>");
        bodyBuilder.AppendLine("<div class='footer'><p>© Mercado Bonsai - Todos os direitos reservados.<br>Este é um e-mail automático enviado para a confirmação cadastral.</p></div>");
        bodyBuilder.AppendLine("</body></html>");

        await EnviarEmailAsync(senderEmail, senderName, destinoEmail, usuarioNome, assunto, bodyBuilder.ToString());
    }

    /// <summary>
    /// Envia e-mail de recuperação/redefinição de senha com link temporário seguro.
    /// </summary>
    public async Task EnviarRecuperacaoSenhaAsync(string destinoEmail, string usuarioNome, string linkRedefinicao)
    {
        if (string.IsNullOrWhiteSpace(destinoEmail)) return;

        string senderEmail = !string.IsNullOrWhiteSpace(_settings.SenderEmail)
            ? _settings.SenderEmail
            : "suporte@mercadobonsai.com.br";
        string senderName = !string.IsNullOrWhiteSpace(_settings.SenderName)
            ? _settings.SenderName
            : "Mercado Bonsai Suporte";
        string assunto = "Recuperação de Senha - Mercado Bonsai";

        var bodyBuilder = new StringBuilder();
        bodyBuilder.AppendLine("<!DOCTYPE html>");
        bodyBuilder.AppendLine("<html lang='pt-BR'>");
        bodyBuilder.AppendLine("<head><meta charset='UTF-8'><style>");
        bodyBuilder.AppendLine("body { font-family: Arial, sans-serif; color: #333; line-height: 1.6; max-width: 650px; margin: 0 auto; padding: 20px; }");
        bodyBuilder.AppendLine(".header { background-color: #4A7C59; color: #fff; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }");
        bodyBuilder.AppendLine(".content { border: 1px solid #e0e0e0; padding: 25px; background: #ffffff; border-radius: 0 0 8px 8px; }");
        bodyBuilder.AppendLine(".btn-reset { display: inline-block; background-color: #4A7C59; color: #ffffff !important; text-decoration: none; padding: 12px 25px; font-weight: bold; border-radius: 6px; margin: 20px 0; }");
        bodyBuilder.AppendLine(".footer { text-align: center; margin-top: 25px; font-size: 12px; color: #777; }");
        bodyBuilder.AppendLine("</style></head>");
        bodyBuilder.AppendLine("<body>");
        bodyBuilder.AppendLine("<div class='header'><h1>🌱 Mercado Bonsai</h1><p>Recuperação de Acesso à Conta</p></div>");
        bodyBuilder.AppendLine("<div class='content'>");
        bodyBuilder.AppendLine($"<p>Olá, <strong>{WebUtility.HtmlEncode(usuarioNome)}</strong>!</p>");
        bodyBuilder.AppendLine("<p>Recebemos uma solicitação para redefinir a senha de acesso à sua conta no <strong>Mercado Bonsai</strong>.</p>");
        bodyBuilder.AppendLine("<p>Clique no botão abaixo para criar uma nova senha. Este link é válido por <strong>2 horas</strong>:</p>");
        bodyBuilder.AppendLine($"<p style='text-align: center;'><a href='{WebUtility.HtmlEncode(linkRedefinicao)}' class='btn-reset'>Redefinir Minha Senha</a></p>");
        bodyBuilder.AppendLine("<p>Se o botão acima não funcionar, você também pode copiar e colar o link abaixo em seu navegador:</p>");
        bodyBuilder.AppendLine($"<p style='word-break: break-all; background: #f8f9fa; padding: 10px; border-radius: 4px; font-size: 13px;'><a href='{WebUtility.HtmlEncode(linkRedefinicao)}' style='color: #4A7C59;'>{WebUtility.HtmlEncode(linkRedefinicao)}</a></p>");
        bodyBuilder.AppendLine("<p style='margin-top: 20px; color: #777; font-size: 13px;'>Se você não solicitou a redefinição de senha, por favor ignore este e-mail. Sua senha atual permanecerá segura e inalterada.</p>");
        bodyBuilder.AppendLine("<p style='margin-top: 30px;'>Atenciosamente,<br><strong>Equipe Mercado Bonsai</strong><br><small>suporte@mercadobonsai.com.br</small></p>");
        bodyBuilder.AppendLine("</div>");
        bodyBuilder.AppendLine("<div class='footer'><p>© Mercado Bonsai - Todos os direitos reservados.<br>Este é um e-mail automático de segurança.</p></div>");
        bodyBuilder.AppendLine("</body></html>");

        await EnviarEmailAsync(senderEmail, senderName, destinoEmail, usuarioNome, assunto, bodyBuilder.ToString());
    }
}
