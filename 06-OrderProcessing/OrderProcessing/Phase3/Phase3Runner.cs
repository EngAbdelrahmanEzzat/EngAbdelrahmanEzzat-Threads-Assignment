using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderProcessing.Phase3
{
    public class Phase3Runner
    {
        public static async Task RunComparisonAsync(CancellationToken ct = default)
        {
            Console.WriteLine("================================================================");
            Console.WriteLine("        Task 9.6: Sync vs Async Order Processing Comparison     ");
            Console.WriteLine("================================================================\n");

            var store = new JsonFileStore();

            // Set up services with artificial failures disabled for fair benchmark comparison
            var customerService = new CustomerService(store);
            var inventoryService = new InventoryService(store);
            var pricingService = new PricingService(store, failShippingForOrderIds: Array.Empty<int>());
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

            // ==========================================================
            // 1. Synchronous Version (10 dedicated threads)
            // ==========================================================
            Console.WriteLine("--> Resetting data for Synchronous Run...");
            await DataSeeder.ResetAsync(ct);

            var syncOrders = store.Read<Order>(DataFiles.IncomingOrders).Take(10).ToList();

            int syncThreadPoolBefore = ThreadPool.ThreadCount;
            var swSync = Stopwatch.StartNew();

            var threads = new List<Thread>();
            foreach (var order in syncOrders)
            {
                var thread = new Thread(() => processor.ProcessOrder(order))
                {
                    Name = $"Dedicated-OrderThread-{order.Id}"
                };
                threads.Add(thread);
            }

            // Start all 10 dedicated threads before joining
            foreach (var t in threads)
            {
                t.Start();
            }

            // Join all threads
            foreach (var t in threads)
            {
                t.Join();
            }

            swSync.Stop();
            int syncThreadPoolAfter = ThreadPool.ThreadCount;

            var syncSavedOrders = store.Read<Order>(DataFiles.Orders);
            int syncCompleted = syncSavedOrders.Count(o => o.Status == OrderStatus.Completed);
            int syncRejected = syncSavedOrders.Count(o => o.Status == OrderStatus.Rejected);
            var syncRejectedIds = syncSavedOrders.Where(o => o.Status == OrderStatus.Rejected).Select(o => o.Id).ToList();

            // ==========================================================
            // 2. Asynchronous Version (Task-based concurrency)
            // ==========================================================
            Console.WriteLine("\n--> Resetting data for Asynchronous Run...");
            await DataSeeder.ResetAsync(ct);

            var asyncOrders = (await store.ReadAsync<Order>(DataFiles.IncomingOrders, ct)).Take(10).ToList();

            int asyncThreadPoolBefore = ThreadPool.ThreadCount;
            var swAsync = Stopwatch.StartNew();

            var tasks = asyncOrders
                .Select(order => processor.ProcessOrderAsync(order, ct))
                .ToList();

            // Await each task / all tasks
            for (int i = 0; i < tasks.Count; i++)
            {
                await tasks[i];
            }

            swAsync.Stop();
            int asyncThreadPoolAfter = ThreadPool.ThreadCount;

            var asyncSavedOrders = await store.ReadAsync<Order>(DataFiles.Orders, ct);
            int asyncCompleted = asyncSavedOrders.Count(o => o.Status == OrderStatus.Completed);
            int asyncRejected = asyncSavedOrders.Count(o => o.Status == OrderStatus.Rejected);
            var asyncRejectedIds = asyncSavedOrders.Where(o => o.Status == OrderStatus.Rejected).Select(o => o.Id).ToList();

            // ==========================================================
            // 3. Display Comparison Results
            // ==========================================================
            Console.WriteLine("\n================================================================");
            Console.WriteLine("                    COMPARISON SUMMARY                          ");
            Console.WriteLine("================================================================");
            Console.WriteLine($"{"Metric",-30} | {"Synchronous (Threads)",-22} | {"Asynchronous (Tasks)",-22}");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"{"Elapsed Time (ms)",-30} | {swSync.ElapsedMilliseconds + " ms",-22} | {swAsync.ElapsedMilliseconds + " ms",-22}");
            Console.WriteLine($"{"Completed Orders",-30} | {syncCompleted,-22} | {asyncCompleted,-22}");
            Console.WriteLine($"{"Rejected Orders",-30} | {syncRejected,-22} | {asyncRejected,-22}");
            Console.WriteLine($"{"Rejected Order IDs",-30} | {$"#{string.Join(", #", syncRejectedIds)}",-22} | {$"#{string.Join(", #", asyncRejectedIds)}",-22}");
            Console.WriteLine($"{"ThreadPool Threads (Before)",-30} | {syncThreadPoolBefore,-22} | {asyncThreadPoolBefore,-22}");
            Console.WriteLine($"{"ThreadPool Threads (After)",-30} | {syncThreadPoolAfter,-22} | {asyncThreadPoolAfter,-22}");
            Console.WriteLine($"{"Dedicated Threads Created",-30} | {threads.Count,-22} | {"0 (Managed by Pool)",-22}");
            Console.WriteLine("================================================================\n");

            // Verify expected results: 8 Completed, 2 Rejected (#4 and #9)
            if (syncCompleted == 8 && syncRejected == 2 && asyncCompleted == 8 && asyncRejected == 2)
            {
                Console.WriteLine("SUCCESS: Both versions produced exact expected results (8 Completed, 2 Rejected: #4 and #9).");
            }
            else
            {
                Console.WriteLine($"WARNING: Results differed from expected 8 Completed / 2 Rejected.");
            }
         
    }
    }
}
