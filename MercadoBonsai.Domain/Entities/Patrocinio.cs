using System;

namespace MercadoBonsai.Domain.Entities;

public class Patrocinio
{
    public int Id { get; set; }
    public string NomeLojaViveiro { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? ImagemUrl { get; set; }
    public string? LinkDestino { get; set; }
    public string Badge { get; set; } = "Patrocinado";
    public int Posicao { get; set; } = 1; // 1=LateralTopo, 2=LateralRodape, 3=HomeBanner
    public bool IsAtivo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
