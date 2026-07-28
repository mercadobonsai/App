using System;

namespace MercadoBonsai.Domain.Entities;

public class Rifa
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Subtitulo { get; set; }
    public string? Descricao { get; set; }
    public string FotoPrincipalUrl { get; set; } = string.Empty;
    public string? FotoDetalheUrl { get; set; }
    public decimal ValorCota { get; set; }
    public int TotalCotas { get; set; }
    public int CotasVendidas { get; set; }
    public int? VendedorId { get; set; }
    public string? VendedorNome { get; set; }
    public DateTime DataSorteio { get; set; }
    public int Status { get; set; } = 1; // 1=Ativa, 2=Sorteada, 3=Cancelada
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public int PorcentagemVendida => TotalCotas > 0 ? (int)Math.Round((double)CotasVendidas / TotalCotas * 100) : 0;
}
