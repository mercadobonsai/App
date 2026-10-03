using System.ComponentModel.DataAnnotations;

namespace MercadoBonsai.Web.Models;

public class RedefinirSenhaViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A nova senha é obrigatória.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter no mínimo {2} caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nova Senha")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "A confirmação de senha é obrigatória.")]
    [DataType(DataType.Password)]
    [Compare("Senha", ErrorMessage = "A senha e a confirmação não conferem.")]
    [Display(Name = "Confirmar Nova Senha")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}
