using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MercadoBonsai.Web.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnviarTermosEPrivacidadeAsync(string destinoEmail, string usuarioNome)
    {
        if (string.IsNullOrWhiteSpace(destinoEmail)) return;

        string senderEmail = _configuration["Smtp:SenderEmail"] ?? "comercial@mercadobonsai.com.br";
        string senderName = _configuration["Smtp:SenderName"] ?? "Mercado Bonsai Comercial";
        string smtpServer = _configuration["Smtp:Server"] ?? "smtp.mercadobonsai.com.br";
        int smtpPort = int.TryParse(_configuration["Smtp:Port"], out int port) ? port : 587;
        string smtpUser = _configuration["Smtp:Username"] ?? "comercial@mercadobonsai.com.br";
        string smtpPass = _configuration["Smtp:Password"] ?? "";
        bool enableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out bool ssl) ? ssl : true;

        string assunto = "Bem-vindo ao Mercado Bonsai - Termos de Uso e Política de Privacidade";

        StringBuilder bodyBuilder = new StringBuilder();
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

        try
        {
            if (string.IsNullOrWhiteSpace(smtpPass))
            {
                _logger.LogInformation("[EmailService] E-mail institucional de Termos e Privacidade gerado com sucesso para {Destino} (Remetente: {Remetente}). Credenciais SMTP pendentes de preenchimento em produção.", 
                    destinoEmail, senderEmail);
                return;
            }

            using var message = new MailMessage();
            message.From = new MailAddress(senderEmail, senderName);
            message.To.Add(new MailAddress(destinoEmail, usuarioNome));
            message.Subject = assunto;
            message.Body = bodyBuilder.ToString();
            message.IsBodyHtml = true;
            message.BodyEncoding = Encoding.UTF8;

            using var client = new SmtpClient(smtpServer, smtpPort);
            client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            client.EnableSsl = enableSsl;

            await client.SendMailAsync(message);
            _logger.LogInformation("[EmailService] E-mail de Termos e Privacidade enviado com sucesso para {Destino} via {SenderEmail}.", destinoEmail, senderEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmailService] Falha ao enviar e-mail de Termos e Privacidade para {Destino}.", destinoEmail);
        }
    }
}
