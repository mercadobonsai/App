using System;

namespace MercadoBonsai.Domain.Entities;

public class FotoProduto
{
    public Guid Id { get; set; }
    public Guid ProdutoId { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsPrincipal { get; set; }
}
