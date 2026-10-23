using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Phase3;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase4
{
    public static class Task10_2_CustomerDashboard
    {
        public static async Task RunAsync(CancellationToken ct = default)
        {
            Console.WriteLine("================================================================");
            Console.WriteLine("           Task 10.2: Customer Dashboard (Task.WhenAll)         ");
            Console.WriteLine("================================================================\n");

            var store = new JsonFileStore();
            var dashboardService = new CustomerDashboardService(store, failPaymentsForCustomerIds: new[] { 99 });

            // 1. Run for Customer #3 (Omar Hassan)
            Console.WriteLine("--> Loading Dashboard for Customer #3...");
            var sw1 = Stopwatch.StartNew();
            try
            {
                var dashboard = await dashboardService.LoadAsync(3, ct);
                sw1.Stop();

                Console.WriteLine($"[Dashboard Loaded in {sw1.ElapsedMilliseconds} ms]");
                Console.WriteLine($"Customer: #{dashboard.Customer.Id} {dashboard.Customer.Name} ({dashboard.Customer.Email})");
                Console.WriteLine($"Orders ({dashboard.Orders.Count}):");
                foreach (var ord in dashboard.Orders)
                {
                    Console.WriteLine($"  - Order #{ord.Id}: Status={ord.Status}, Amount=${ord.TotalAmount:F2}");
                }
                Console.WriteLine($"Payments ({dashboard.Payments.Count}):");
                foreach (var p in dashboard.Payments)
                {
                    Console.WriteLine($"  - Payment for Order #{p.OrderId}: Status={p.Status}, Amount=${p.Amount:F2}");
                }
                Console.WriteLine($"Emails ({dashboard.Emails.Count}):");
                foreach (var e in dashboard.Emails)
                {
                    Console.WriteLine($"  - Email for Order #{e.OrderId}: Subject='{e.Subject}', Attempts={e.Attempts}");
                }
            }
            catch (Exception ex)
            {
                sw1.Stop();
                Console.WriteLine($"Error loading dashboard for Customer #3: {ex.Message}");
            }

            Console.WriteLine("\n" + new string('-', 64) + "\n");

            // 2. Run for Customer #99 (Multiple Failures)
            Console.WriteLine("--> Loading Dashboard for Customer #99 (Expect Multiple Failures)...");
            var sw2 = Stopwatch.StartNew();
            try
            {
                var dashboard99 = await dashboardService.LoadAsync(99, ct);
                sw2.Stop();
                Console.WriteLine($"Unexpected success for #99 in {sw2.ElapsedMilliseconds} ms");
            }
            catch (AggregateException aggEx)
            {
                sw2.Stop();
                Console.WriteLine($"[Completed in {sw2.ElapsedMilliseconds} ms with {aggEx.InnerExceptions.Count} failures reported:]");
                foreach (var inner in aggEx.InnerExceptions)
                {
                    Console.WriteLine($"  * Failure: {inner.GetType().Name}: {inner.Message}");
                }
            }
            catch (Exception ex)
            {
                sw2.Stop();
                Console.WriteLine($"[Completed in {sw2.ElapsedMilliseconds} ms with single failure reported:]");
                Console.WriteLine($"  * Failure: {ex.GetType().Name}: {ex.Message}");
            }

            Console.WriteLine();
        }
    }
}
