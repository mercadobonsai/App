using System;
using System.ComponentModel.DataAnnotations;
using MercadoBonsai.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace MercadoBonsai.Web.Models;

public class PerfilViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [Display(Name = "Nome Completo")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Telefone de Contato / WhatsApp")]
    public string? Telefone { get; set; }

    public PerfilUsuario Perfil { get; set; }

    // Dados Fiscais
    [Display(Name = "Razão Social / Nome Fantasia")]
    public string? RazaoSocial { get; set; }

    [Display(Name = "CPF / CNPJ")]
    public string? CpfCnpj { get; set; }

    [Display(Name = "Inscrição Estadual")]
    public string? InscricaoEstadual { get; set; }

    [Display(Name = "Data de Nascimento")]
    [DataType(DataType.Date)]
    public DateTime? DataNascimento { get; set; }

    [Display(Name = "Renda Mensal / Faturamento Estimado (R$)")]
    [Range(0, 999999999, ErrorMessage = "Informe um valor de renda/faturamento válido.")]
    public decimal? RendaFaturamento { get; set; }

    // Endereço Completo & Origem do Envio (Melhor Envio)
    [Display(Name = "CEP de Origem / Envio")]
    public string? Cep { get; set; }

    [Display(Name = "Logradouro / Endereço")]
    public string? Logradouro { get; set; }

    [Display(Name = "Número")]
    public string? Numero { get; set; }

    [Display(Name = "Complemento")]
    public string? Complemento { get; set; }

    [Display(Name = "Bairro")]
    public string? Bairro { get; set; }

    [Display(Name = "Cidade")]
    public string? Cidade { get; set; }

    [Display(Name = "Estado (UF)")]
    public string? Estado { get; set; }

    // Dados Financeiros
    [Display(Name = "Chave PIX")]
    public string? ChavePix { get; set; }

    [Display(Name = "Banco")]
    public string? Banco { get; set; }

    [Display(Name = "Agência")]
    public string? Agencia { get; set; }

    [Display(Name = "Conta")]
    public string? Conta { get; set; }

    // Apresentação do Viveiro
    [Display(Name = "Descrição do Viveiro")]
    public string? DescricaoViveiro { get; set; }

    public string? LogotipoUrl { get; set; }

    [Display(Name = "Alterar Logotipo do Viveiro")]
    public IFormFile? LogotipoArquivo { get; set; }

    // Expansão: Plano Pago & Cartão de Visitas Digital
    public int PlanoId { get; set; } = 1;
    public string NomePlano { get; set; } = "Bronze";
    public bool LiberarCartaoVisitas { get; set; } = false;

    [Display(Name = "Isento de Cobrança (Não cobrar)")]
    public bool IsentoCobranca { get; set; } = false;

    public string? LinkVitrineCartao { get; set; }
    public string? LinkInsumosCartao { get; set; }
    public string? LinkVasosCartao { get; set; }
    public string? LinkEngajamentoCartao { get; set; }

    // Expansão: Integração Asaas & Retenção de Comissão
    public string? AsaasCustomerId { get; set; }
    public string? AsaasAccountId { get; set; }
    public string? AsaasSubscriptionId { get; set; }

    [Display(Name = "Comissão / Retenção Personalizada (%)")]
    public decimal? PercentualRetencaoPersonalizado { get; set; }

    [Display(Name = "URL de Webhook Personalizada (e-vendas)")]
    [Url(ErrorMessage = "Informe uma URL válida com http:// ou https://")]
    public string? WebhookUrl { get; set; }

    // Auditoria
    public DateTime? DataUltimaAlteracao { get; set; }
    public string? UsuarioAlteracaoNome { get; set; }
}
