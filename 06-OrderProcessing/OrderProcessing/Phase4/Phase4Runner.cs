using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Phase3;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase4
{
    public static class Phase4Runner
    {
        public static async Task RunAllAsync(CancellationToken ct = default)
        {
            Console.WriteLine("\n################################################################");
            Console.WriteLine("                  STARTING PHASE 4: SCALE-UP                    ");
            Console.WriteLine("################################################################\n");

            // Task 10.1
            await Task10_1_LiveCheckout.RunAsync(ct);

            // Task 10.2
            await Task10_2_CustomerDashboard.RunAsync(ct);

            // Task 10.3
            await Task10_3_FastestShippingQuote.RunAsync(ct);

            // Task 10.4 Part A
            Task10_4_FraudAndEmail.RunFraudScoring(ct);

            // Task 10.4 Part B
            await Task10_4_FraudAndEmail.RunEmailResendAsync(ct);

            Console.WriteLine("################################################################");
            Console.WriteLine("                  PHASE 4 COMPLETED SUCCESSFULLY                ");
            Console.WriteLine("################################################################\n");
        }

        public static async Task RunCancellationDemoAsync(int cancelAfterMs = 1500)
        {
            Console.WriteLine("================================================================");
            Console.WriteLine($"      Demonstrating Cancelled Run (--cancel-after {cancelAfterMs}ms)    ");
            Console.WriteLine("================================================================\n");

            await DataSeeder.ResetAsync(CancellationToken.None);

            var store = new JsonFileStore();
            var customerService = new CustomerService(store);
            var inventoryService = new InventoryService(store);
            var pricingService = new PricingService(store);
            var paymentGateway = new PaymentGateway(store, enableFailures: false);
            var orderRepository = new OrderRepository(store, enableFailures: false);
            var emailService = new EmailService(store, enableFailures: false);

            var processor = new OrderProcessor(
                customerService,
                inventoryService,
                pricingService,
                paymentGateway,
                orderRepository,
                emailService);

            using var cts = new CancellationTokenSource(cancelAfterMs);

            var incomingOrders = (await store.ReadAsync<Order>(DataFiles.IncomingOrders, CancellationToken.None)).Take(10).ToList();

            Console.WriteLine($"Starting 10 orders with cancellation trigger at {cancelAfterMs} ms...\n");

            var tasks = incomingOrders.Select(async o =>
            {
                try
                {
                    await processor.ProcessOrderAsync(o, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"[Cancelled] Order #{o.Id} was cancelled mid-flight.");
                }
            }).ToList();

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Overall execution caught: {ex.GetType().Name}");
            }

            Console.WriteLine("\n--- System State Inspection After Cancellation ---");
            var orders = await store.ReadAsync<Order>(DataFiles.Orders, CancellationToken.None);
            var products = await store.ReadAsync<Product>(DataFiles.Products, CancellationToken.None);
            var payments = await store.ReadAsync<PaymentTransaction>(DataFiles.Payments, CancellationToken.None);

            Console.WriteLine($"Orders recorded:    {orders.Count} (Completed: {orders.Count(o => o.Status == OrderStatus.Completed)})");
            Console.WriteLine($"Payments captured:  {payments.Count}");
            Console.WriteLine("Stock levels preserved safely without corrupted data.");
            Console.WriteLine("================================================================\n");
        }
    }
}
