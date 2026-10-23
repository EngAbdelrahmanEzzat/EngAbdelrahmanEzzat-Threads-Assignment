using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase4
{
    public interface IShippingCarrier
    {
        string Name { get; }
        Task<decimal> GetQuoteAsync(Order order, CancellationToken ct);
    }

    public class ShippingQuote
    {
        public string Carrier { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public long ElapsedMs { get; set; }
        public List<string> CancelledCarriers { get; set; } = new();
    }
}
