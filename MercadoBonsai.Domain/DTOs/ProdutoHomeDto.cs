using System;
using MercadoBonsai.Domain.Enums;

namespace MercadoBonsai.Domain.DTOs;

public class ProdutoHomeDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public decimal ValorVenda { get; set; }
    public ModalidadeEntrega TipoModalidade { get; set; }
    public string Especie { get; set; } = string.Empty;
    public string FotoCapaUrl { get; set; } = string.Empty;
}
