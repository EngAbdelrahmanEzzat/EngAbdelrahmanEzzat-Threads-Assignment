using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Phase2;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase4
{
    public static class Task10_3_FastestShippingQuote
    {
        public static async Task RunAsync(CancellationToken ct = default)
        {
            Console.WriteLine("================================================================");
            Console.WriteLine("       Task 10.3: Fastest Shipping Quote (Task.WhenAny)         ");
            Console.WriteLine("================================================================\n");

            var store = new JsonFileStore();
            var incomingOrders = (await store.ReadAsync<Order>(DataFiles.IncomingOrders, ct)).Take(5).ToList();

            var shippingService = new ShippingQuoteService();

            // 1. Run for orders #1–#5
            Console.WriteLine("--> Getting fastest quote for Orders #1–#5 (Budget: 1.2s):\n");

            foreach (var order in incomingOrders)
            {
                try
                {
                    var quote = await shippingService.GetFastestQuoteAsync(order, ct);
                    string cancelledInfo = quote.CancelledCarriers.Count > 0
                        ? $"[Cancelled: {string.Join(", ", quote.CancelledCarriers)}]"
                        : "[None cancelled]";

                    Console.WriteLine($"Order #{order.Id}: Winner='{quote.Carrier}', Price=${quote.Price:F2}, Time={quote.ElapsedMs} ms {cancelledInfo}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Order #{order.Id}: Failed - {ex.Message}");
                }
            }

            Console.WriteLine("\n" + new string('-', 64) + "\n");

            // 2. Separate run for Order #6 where every carrier is configured to fail
            Console.WriteLine("--> Test Run for Order #6 with ALL carriers failing (Expect: All carriers failed):");
            var failingCarriers = new List<IShippingCarrier>
            {
                new FastShip(failForOrderIds: new[] { 6 }),
                new CairoExpress(failForOrderIds: new[] { 6 }),
                new NileCourier(failForOrderIds: new[] { 6 })
            };

            var allFailingService = new ShippingQuoteService(failingCarriers);
            var order6 = new Order { Id = 6, SubTotal = 45m };

            try
            {
                var quote6 = await allFailingService.GetFastestQuoteAsync(order6, ct);
                Console.WriteLine($"Unexpected success for Order #6: {quote6.Carrier}");
            }
            catch (ShippingProviderUnavailableException ex)
            {
                Console.WriteLine($"Successfully caught expected exception: {ex.GetType().Name} -> '{ex.Message}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Caught unexpected exception: {ex.GetType().Name} -> '{ex.Message}'");
            }

            Console.WriteLine();
        }
    }
}
