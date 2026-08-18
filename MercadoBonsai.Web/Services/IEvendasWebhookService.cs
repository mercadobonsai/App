using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Web.Services;

public interface IEvendasWebhookService
{
    Task<bool> NotificarMudancaStatusAsync(Pedido pedido);
}
