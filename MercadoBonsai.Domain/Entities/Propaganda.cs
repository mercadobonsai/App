using System;

namespace MercadoBonsai.Domain.Entities;

public class Propaganda
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNome { get; set; } = string.Empty;
    public string TipoEspaco { get; set; } = "Economico"; // 'Economico', 'Basico', 'Intermediario', 'Avancado'
    public decimal PrecoMensal { get; set; }
    public string? ImagemUrl { get; set; }
    public string? LinkDestino { get; set; }
    public string Status { get; set; } = "Pendente"; // 'Pendente', 'Ativo', 'Expirado', 'Rejeitado'
    public DateTime? DataInicio { get; set; }
    public DateTime? DataExpiracao { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
