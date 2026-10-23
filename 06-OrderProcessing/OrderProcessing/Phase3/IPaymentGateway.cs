using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase3
{
    public interface IPaymentGateway
    {
        Task<PaymentResult> ProcessPaymentAsync(Order order, CancellationToken ct = default);
        PaymentResult ProcessPayment(Order order);
    }
}
