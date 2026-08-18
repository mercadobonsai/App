namespace MercadoBonsai.Domain.Enums;

public static class StatusPedido
{
    public const string Criado = "Criado";
    public const string AguardandoAceite = "Aguardando Aceite";
    public const string Recusado = "Recusado";
    public const string AguardandoPagamento = "Aguardando Pagamento";
    public const string Pago = "Pago";
    public const string EmDespacho = "Em Despacho";
    public const string EmTransito = "Em Transito";
    public const string Entregue = "Entregue";
    public const string Conferido = "Conferido";
    public const string Finalizado = "Finalizado";

    public static readonly string[] Todos = new[]
    {
        Criado,
        AguardandoAceite,
        Recusado,
        AguardandoPagamento,
        Pago,
        EmDespacho,
        EmTransito,
        Entregue,
        Conferido,
        Finalizado
    };
}
