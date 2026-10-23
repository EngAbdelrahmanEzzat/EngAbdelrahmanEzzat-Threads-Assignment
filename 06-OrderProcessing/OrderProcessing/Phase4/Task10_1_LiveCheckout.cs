using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Phase3;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase4
{
    public static class Task10_1_LiveCheckout
    {
        public record OrderResult(Order Order, TimeSpan Elapsed, bool Success);

        public static async Task RunAsync(CancellationToken ct = default)
        {
            Console.WriteLine("================================================================");
            Console.WriteLine("       Task 10.1: Live Checkout Progress (Task.WhenEach)       ");
            Console.WriteLine("================================================================\n");

            // Reset initial data
            await DataSeeder.ResetAsync(ct);

            var store = new JsonFileStore();

            // Configure Phase 3 failure injection (Task 9.2)
            var customerService = new CustomerService(store);
            var inventoryService = new InventoryService(store);
            var pricingService = new PricingService(store, failShippingForOrderIds: new[] { 7 });
            var paymentGateway = new PaymentGateway(store, timeoutOrderIds: new[] { 5 }, declineOrderIds: new[] { 8 });
            var orderRepository = new OrderRepository(store, failSaveForOrderIds: new[] { 13 });
            var emailService = new EmailService(store, failAttempts: new Dictionary<int, int>
            {
                [3] = 2,
                [17] = int.MaxValue
            });

            var processor = new OrderProcessor(
                customerService,
                inventoryService,
                pricingService,
                paymentGateway,
                orderRepository,
                emailService);

            // Read all 20 incoming orders
            var incomingOrders = await store.ReadAsync<Order>(DataFiles.IncomingOrders, ct);

            Console.WriteLine($"Starting checkout for all {incomingOrders.Count} orders at once...\n");

            var globalStopwatch = Stopwatch.StartNew();

            // R1: Start all 20 orders at once
            var orderTasks = incomingOrders.Select(async order =>
            {
                var sw = Stopwatch.StartNew();
                bool success = false;
                try
                {
                    success = await processor.ProcessOrderAsync(order, ct);
                }
                catch (Exception ex)
                {
                    order.Status = OrderStatus.Failed;
                    order.FailureReason = ex.Message;
                }
                finally
                {
                    sw.Stop();
                }
                return new OrderResult(order, sw.Elapsed, success);
            }).ToList();

            // R2, R3, R4: Use Task.WhenEach to stream tasks the moment they finish
            int completedCount = 0;
            int total = orderTasks.Count;

            await foreach (var completedTask in Task.WhenEach(orderTasks))
            {
                completedCount++;
                var result = await completedTask;
                var order = result.Order;

                string reason = string.IsNullOrEmpty(order.FailureReason)
                    ? string.Empty
                    : $" ({order.FailureReason})";

                // Format: [ 7/20] Order #12 -> Rejected (Coupon 'OLD20' has expired) after 2.4 s
                Console.WriteLine($"[{completedCount,2}/{total}] Order #{order.Id,-2} -> {order.Status}{reason} after {result.Elapsed.TotalSeconds:F1} s");
            }

            globalStopwatch.Stop();
            Console.WriteLine($"\nAll orders finished in {globalStopwatch.Elapsed.TotalSeconds:F2} s.");

            // R5: Print counts per status (matching 9.4)
            var savedOrders = await store.ReadAsync<Order>(DataFiles.Orders, ct);
            var completedOrders = savedOrders.Count(o => o.Status == OrderStatus.Completed);
            var rejectedOrders = savedOrders.Count(o => o.Status == OrderStatus.Rejected);
            var failedOrders = savedOrders.Count(o => o.Status == OrderStatus.Failed);
            var paymentFailedOrders = savedOrders.Count(o => o.Status == OrderStatus.PaymentFailed);

            Console.WriteLine("\n--- Final Status Summary (Task 9.4 Verification) ---");
            Console.WriteLine($"Completed:      {completedOrders}");
            Console.WriteLine($"Rejected:       {rejectedOrders}");
            Console.WriteLine($"Failed:         {failedOrders}");
            Console.WriteLine($"Payment Failed: {paymentFailedOrders}");
            Console.WriteLine($"Total Saved:    {savedOrders.Count} / {total}\n");
        }
    }
}
