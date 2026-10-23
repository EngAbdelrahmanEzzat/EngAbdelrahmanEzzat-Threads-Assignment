using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Phase2;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase4
{
    public class ShippingQuoteService
    {
        private readonly List<IShippingCarrier> _carriers;

        public ShippingQuoteService(IEnumerable<IShippingCarrier>? carriers = null)
        {
            _carriers = carriers != null
                ? carriers.ToList()
                : new List<IShippingCarrier>
                {
                    new FastShip(),
                    new CairoExpress(),
                    new NileCourier()
                };
        }

        public async Task<ShippingQuote> GetFastestQuoteAsync(Order order, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(order);

            var sw = Stopwatch.StartNew();

            // R4 & R7: 1.2s time budget linked to caller's cancellation token
            using var budgetCts = new CancellationTokenSource(TimeSpan.FromSeconds(1.2));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, budgetCts.Token);

            var carrierTasks = new Dictionary<Task<decimal>, IShippingCarrier>();

            // R1: Ask all three carriers at the same time
            foreach (var carrier in _carriers)
            {
                var task = carrier.GetQuoteAsync(order, linkedCts.Token);
                carrierTasks[task] = carrier;
            }

            var remainingTasks = carrierTasks.Keys.ToList();
            int failedCount = 0;
            int totalCarriers = _carriers.Count;

            // R2: Use the first successful quote to arrive
            while (remainingTasks.Count > 0)
            {
                Task<decimal> finishedTask;
                try
                {
                    // Wait for any carrier to finish
                    finishedTask = await Task.WhenAny(remainingTasks);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                remainingTasks.Remove(finishedTask);
                var carrier = carrierTasks[finishedTask];

                if (finishedTask.IsCompletedSuccessfully)
                {
                    sw.Stop();
                    decimal quotePrice = finishedTask.Result;

                    // R3: Cancel all remaining carriers
                    var cancelledCarriers = remainingTasks.Select(t => carrierTasks[t].Name).ToList();
                    linkedCts.Cancel();

                    return new ShippingQuote
                    {
                        Carrier = carrier.Name,
                        Price = quotePrice,
                        ElapsedMs = sw.ElapsedMilliseconds,
                        CancelledCarriers = cancelledCarriers
                    };
                }
                else
                {
                    // Failed carrier, count failure and keep waiting for other carriers
                    failedCount++;
                }

                // R6: If all carriers failed within budget, throw exception
                if (failedCount == totalCarriers)
                {
                    sw.Stop();
                    throw new ShippingProviderUnavailableException("All carriers failed");
                }
            }

            sw.Stop();

            // R4: Time budget exceeded without successful quote -> Flat rate fallback
            linkedCts.Cancel();
            var timedOutCarriers = _carriers.Select(c => c.Name).ToList();

            return new ShippingQuote
            {
                Carrier = "Flat rate",
                Price = 25.00m,
                ElapsedMs = sw.ElapsedMilliseconds,
                CancelledCarriers = timedOutCarriers
            };
        }
    }
}
