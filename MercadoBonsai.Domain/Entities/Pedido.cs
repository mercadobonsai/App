using System;

namespace MercadoBonsai.Domain.Entities;

public class Pedido
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public int CompradorId { get; set; }
    public int VendedorId { get; set; }
    public int ProdutoId { get; set; }
    public string StatusPedido { get; set; } = Domain.Enums.StatusPedido.Criado;
    public string TipoPagamento { get; set; } = "PIX / Asaas";
    public DateTime DataPedido { get; set; } = DateTime.Now;
    public DateTime? DataPagamento { get; set; }
    public decimal ValorPedido { get; set; }
    public decimal? ValorFrete { get; set; } = 0.00m;
    public decimal? ValorSeguro { get; set; } = 0.00m;
    public decimal ValorTotal { get; set; }
    public string? UrlCheckout { get; set; }
    public string? Observacao { get; set; }
    public string? CodigoRastreio { get; set; }
    public string? UrlRastreio { get; set; }
    public string? CompradorNome { get; set; }
    public string? CompradorEmail { get; set; }
    public string? CompradorTelefone { get; set; }
    public string? CompradorEndereco { get; set; }
    public DateTime? CompradorAniversario { get; set; }
    public string? UrlAvaliacao { get; set; }
    public string? AsaasPaymentId { get; set; }
    public int? LeilaoId { get; set; }
    public int? PosicaoVencedorLeilao { get; set; } = 1;

    // Propriedades auxiliares de junção (Join/DTO)
    public string? ProdutoNome { get; set; }
    public string? ProdutoImagemUrl { get; set; }
    public string? VendedorNome { get; set; }
    public string? VendedorTelefone { get; set; }
}
