using System;

namespace MercadoBonsai.Domain.Entities;

public class ProntuarioPlanta
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string NomePopular { get; set; } = string.Empty;
    public string? NomeCientifico { get; set; }
    public string Especie { get; set; } = string.Empty;
    public decimal Altura { get; set; }
    public decimal Largura { get; set; }
    public decimal Comprimento { get; set; }
    public decimal Peso { get; set; }
    public string? DescricaoLivre { get; set; }
    public string? FotoPrincipalUrl { get; set; }
    public DateTime DataInicial { get; set; } = DateTime.Now;
    public DateTime? DataUltimaManutencao { get; set; }
    public DateTime? DataProximaManutencao { get; set; }
    public DateTime? DataUltimaAdubacao { get; set; }
    public DateTime? DataProximaAdubacao { get; set; }
    
    // Controle de Concorrência (Lock de Edição Simultânea)
    public int? LockUsuarioId { get; set; }
    public string? LockUsuarioNome { get; set; }
    public DateTime? LockTimestamp { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
}
