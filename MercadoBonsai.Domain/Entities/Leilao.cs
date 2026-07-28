using System;
using System.Collections.Generic;

namespace MercadoBonsai.Domain.Entities;

public class Leilao
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Subtitulo { get; set; }
    public string? Descricao { get; set; }
    public string FotoPrincipalUrl { get; set; } = string.Empty;
    public string? FotoDetalheUrl { get; set; }
    public string? Badge { get; set; }
    public decimal LanceAtual { get; set; }
    public decimal ProximoLanceMinimo { get; set; }
    public decimal IncrementoMinimo { get; set; } = 50.00m;
    public int? VendedorId { get; set; }
    public string? VendedorNome { get; set; }
    public DateTime DataFinalizacao { get; set; }
    public int Status { get; set; } = 1; // 1=Ativo, 2=Finalizado, 3=Cancelado
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public List<LanceLeilao> Lances { get; set; } = new();
}
