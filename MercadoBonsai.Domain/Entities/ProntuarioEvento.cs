using System;

namespace MercadoBonsai.Domain.Entities;

public class ProntuarioEvento
{
    public int Id { get; set; }
    public int PlantaId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public DateTime DataEvento { get; set; } = DateTime.Now;
    
    // Recursos para Planos Pagos
    public string? FotoUrl { get; set; }
    public string? NomeAdubo { get; set; }
    public string? NomeRemedio { get; set; }
    
    public DateTime DataCriacao { get; set; } = DateTime.Now;
}
