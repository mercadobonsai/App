using System;

namespace MercadoBonsai.Domain.Entities;

public class LanceLeilao
{
    public int Id { get; set; }
    public int LeilaoId { get; set; }
    public int? UsuarioId { get; set; }
    public string UsuarioNome { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime DataLance { get; set; } = DateTime.UtcNow;
}
