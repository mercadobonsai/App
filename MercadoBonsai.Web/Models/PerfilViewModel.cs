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

    [Display(Name = "Telefone")]
    public string? Telefone { get; set; }

    public PerfilUsuario Perfil { get; set; }

    // Dados Fiscais
    [Display(Name = "Razão Social")]
    public string? RazaoSocial { get; set; }

    [Display(Name = "CPF / CNPJ")]
    public string? CpfCnpj { get; set; }

    [Display(Name = "Inscrição Estadual")]
    public string? InscricaoEstadual { get; set; }

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

    // Auditoria
    public DateTime? DataUltimaAlteracao { get; set; }
    public string? UsuarioAlteracaoNome { get; set; }
}
