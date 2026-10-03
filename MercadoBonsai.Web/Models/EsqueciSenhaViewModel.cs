using System.ComponentModel.DataAnnotations;

namespace MercadoBonsai.Web.Models;

public class EsqueciSenhaViewModel
{
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [Display(Name = "E-mail cadastrado")]
    public string Email { get; set; } = string.Empty;
}
