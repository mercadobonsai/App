using System;

namespace MercadoBonsai.Domain.Entities;

public class PedidoRifa
{
    public int Id { get; set; }
    public int RifaId { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNome { get; set; } = string.Empty;
    public int QuantidadeCotas { get; set; }
    public decimal ValorTotal { get; set; }
    public string ChavePix { get; set; } = string.Empty;
    public string Status { get; set; } = "Pendente";
    public DateTime DataReserva { get; set; } = DateTime.UtcNow;
}
