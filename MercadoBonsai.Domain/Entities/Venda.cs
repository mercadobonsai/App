using System;
using MercadoBonsai.Domain.Enums;

namespace MercadoBonsai.Domain.Entities;

public class Venda
{
    public Guid Id { get; set; }
    public Guid CompradorId { get; set; }
    public Guid ProdutoId { get; set; }
    public decimal ValorTotal { get; set; }

    // Expansão Frete & Seguro (Requirement 3)
    public decimal? ValorFrete { get; set; }
    public decimal? ValorSeguro { get; set; }

    public DateTime DataVenda { get; set; }
    public StatusVenda Status { get; set; }
    public ModalidadePagamento ModalidadePagamento { get; set; }
    public ModalidadeEntrega ModalidadeEntrega { get; set; }
}
