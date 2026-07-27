using System.ComponentModel.DataAnnotations;
using MercadoBonsai.Domain.Enums;

namespace MercadoBonsai.Web.Models;

public class CadastroViewModel
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [Display(Name = "Nome completo")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme sua senha.")]
    [DataType(DataType.Password)]
    [Compare("Senha", ErrorMessage = "As senhas não coincidem.")]
    [Display(Name = "Confirmar senha")]
    public string ConfirmarSenha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecione um perfil.")]
    [Display(Name = "Tipo de conta")]
    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Comprador;
}
