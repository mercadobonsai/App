using System;

namespace MercadoBonsai.Domain.Entities;

public class DicaCultivo
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Subtitulo { get; set; }
    public string Conteudo { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public bool IsAtiva { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
