using System;
using MercadoBonsai.Domain.Enums;

namespace MercadoBonsai.Domain.Entities;

public class Produto
{
    public int Id { get; set; }
    public int VendedorId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public int QuantidadeEstoque { get; set; }
    public string ImagemUrl { get; set; } = string.Empty;
    public StatusProduto Status { get; set; } = StatusProduto.Disponivel;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
