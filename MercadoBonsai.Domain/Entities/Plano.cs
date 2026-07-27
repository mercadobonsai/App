namespace MercadoBonsai.Domain.Entities;

public class Plano
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int LimiteAnuncios { get; set; }
}
