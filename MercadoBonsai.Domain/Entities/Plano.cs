namespace MercadoBonsai.Domain.Entities;

public class Plano
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public decimal PercentualComissao { get; set; } = 10.00m;
    public int LimiteRifas30Dias { get; set; } = 2;
    public int LimiteLeiloes30Dias { get; set; } = 2;
    public int LimiteAnuncios { get; set; } = 10;
    public bool DestaquesHome { get; set; } = false;
}
