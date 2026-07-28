using System.ComponentModel.DataAnnotations;
using MercadoBonsai.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace MercadoBonsai.Web.Models;

public class CriarProdutoViewModel
{
    [Required(ErrorMessage = "O nome do produto é obrigatório.")]
    [StringLength(150, ErrorMessage = "O nome não pode exceder 150 caracteres.")]
    [Display(Name = "Nome do Bonsai / Produto")]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "Descrição Detalhada")]
    public string? Descricao { get; set; }

    [Required(ErrorMessage = "O preço é obrigatório.")]
    [Range(0.01, 999999.99, ErrorMessage = "Informe um valor válido maior que zero.")]
    [Display(Name = "Preço (R$)")]
    public decimal Preco { get; set; }

    [Required(ErrorMessage = "A quantidade em estoque é obrigatória.")]
    [Range(0, 9999, ErrorMessage = "Informe uma quantidade válida.")]
    [Display(Name = "Quantidade em Estoque")]
    public int QuantidadeEstoque { get; set; } = 1;

    [Display(Name = "Status da Oferta")]
    public StatusProduto Status { get; set; } = StatusProduto.Disponivel;

    [Display(Name = "Foto do Produto")]
    public IFormFile? Imagem { get; set; }
}
