using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Phase2;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase4
{
    public abstract class BaseShippingCarrier : IShippingCarrier
    {
        public abstract string Name { get; }
        public abstract int MinDelayMs { get; }
        public abstract int MaxDelayMs { get; }
        public abstract decimal PriceFactor { get; }

        private readonly HashSet<int> _failForOrderIds;

        protected BaseShippingCarrier(IEnumerable<int>? failForOrderIds = null)
        {
            _failForOrderIds = failForOrderIds != null ? new HashSet<int>(failForOrderIds) : new HashSet<int>();
        }

        public async Task<decimal> GetQuoteAsync(Order order, CancellationToken ct)
        {
            try
            {
                // Handshake 150ms
                await Task.Delay(150, ct);

                // Check injected failure
                if (_failForOrderIds.Contains(order.Id))
                {
                    throw new ShippingProviderUnavailableException($"Carrier {Name} failed for Order #{order.Id}");
                }

                CheckCarrierSpecificFailure(order);

                // Simulated quote calculation latency
                int delay = Random.Shared.Next(MinDelayMs, MaxDelayMs + 1);
                await Task.Delay(delay, ct);

                decimal basePrice = CalculateBaseShipping(order);
                return Math.Round(basePrice * PriceFactor, 2, MidpointRounding.AwayFromZero);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[{Name}] cancelled");
                throw;
            }
        }

        protected virtual void CheckCarrierSpecificFailure(Order order)
        {
        }

        private static decimal CalculateBaseShipping(Order order)
        {
            if (order.SubTotal >= 1000m)
            {
                return 0m;
            }

            int totalQuantity = order.Items.Sum(i => i.Quantity);
            // Rough weights based on products.json: 0.5kg average if not loaded
            decimal totalWeight = order.Items.Sum(i => (i.Quantity * 0.5m));

            decimal shippingCost = 5.00m + 1.50m * totalQuantity + 2.00m * totalWeight;
            return Math.Round(shippingCost, 2, MidpointRounding.AwayFromZero);
        }
    }

    public class FastShip : BaseShippingCarrier
    {
        public override string Name => "FastShip";
        public override int MinDelayMs => 500;
        public override int MaxDelayMs => 1500;
        public override decimal PriceFactor => 1.00m;

        public FastShip(IEnumerable<int>? failForOrderIds = null) : base(failForOrderIds) { }
    }

    public class CairoExpress : BaseShippingCarrier
    {
        public override string Name => "CairoExpress";
        public override int MinDelayMs => 300;
        public override int MaxDelayMs => 2000;
        public override decimal PriceFactor => 0.95m;

        public CairoExpress(IEnumerable<int>? failForOrderIds = null) : base(failForOrderIds) { }

        protected override void CheckCarrierSpecificFailure(Order order)
        {
            // Fails for even order IDs
            if (order.Id % 2 == 0)
            {
                throw new ShippingProviderUnavailableException($"Carrier {Name} unavailable for even Order #{order.Id}");
            }
        }
    }

    public class NileCourier : BaseShippingCarrier
    {
        public override string Name => "NileCourier";
        public override int MinDelayMs => 800;
        public override int MaxDelayMs => 1200;
        public override decimal PriceFactor => 1.10m;

        public NileCourier(IEnumerable<int>? failForOrderIds = null) : base(failForOrderIds) { }
    }
}
