using OrderProcessing.Phase1;
using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class Phase2Runner
    {
        private readonly JsonFileStore _store;
        private readonly RaceConditionDemo _raceconditiondemo;
        private readonly OrderProcessorV2 _orderprocessor;
        private readonly OrderPricer _orderpricer;
        private readonly OrderValidator _ordervalidator;
        private readonly OrderRepository _orderrepository;
        private readonly ReportGenerator _reprtgenerator;
        public Phase2Runner(OrderProcessorV2 orderprocessor
            , JsonFileStore store
            ,RaceConditionDemo raceConditionDemo
            , OrderPricer orderpricer
            , OrderValidator ordervalidator
            , OrderRepository orderrepository
            ,ReportGenerator reprtgenerator)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _raceconditiondemo = raceConditionDemo ?? throw new ArgumentNullException(nameof(raceConditionDemo));
            _orderprocessor = orderprocessor ?? throw new ArgumentNullException(nameof(orderprocessor));
            _orderpricer = orderpricer ?? throw new ArgumentNullException(nameof(orderpricer));
            _ordervalidator = ordervalidator ?? throw new ArgumentNullException(nameof(ordervalidator));
            _orderrepository = orderrepository ?? throw new ArgumentNullException(nameof(orderrepository));
            _reprtgenerator = reprtgenerator ?? throw new ArgumentNullException(nameof(reprtgenerator));
        }

        public async Task RunAsync(CancellationToken ct = default)
        {
           
            await DataSeeder.ResetAsync(ct);

            await _raceconditiondemo.RunAsync(ct);

            await DataSeeder.ResetAsync(ct);

            var orders = await _store.ReadAsync<Order>(DataFiles.IncomingOrders, ct);
            Console.WriteLine(orders.Count);
            await _orderpricer.PriceOrderSequentialAsync(orders[0], ct);

           
            foreach (var item in orders)
            {
                //ده بيعمل فاليدات وبيعمل برضو سيف
                await _ordervalidator.ValidateOrderAsync(item, ct);

            }
            // Start measuring processing time.
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<Order> validatedOrders = orders.Where(order => order.Status == OrderStatus.Validated).ToList();

            List<Task<bool>> tasks = validatedOrders.Select(order => _orderprocessor.ProcessValidatedOrderAsync(order, ct)).ToList();
            // Step 7: Print the status of the first task immediately.
            if (tasks.Count > 0)
            { Console.WriteLine($"Task[0] Status: {tasks[0].Status}"); }

            // Step 8: Wait 1 second and print the status of every task.
            await Task.Delay(1000, ct);

            Console.WriteLine("\n--- Task Status After 1 Second ---");

            for (int i = 0; i < tasks.Count; i++)
            {
                Console.WriteLine($"Task[{i}] Status: {tasks[i].Status}");
            }

            // Step 9: Await each task independently.
            // A failure in one order must not stop processing the others.
            Console.WriteLine("\n--- Processing Results ---");

            for (int i = 0; i < tasks.Count; i++)
            {
                try
                {
                    await tasks[i];
                }
                catch (Exception ex)
                {
                    Order order = validatedOrders[i];

                    Console.WriteLine($"Task[{i}] Status: {tasks[i].Status}");
                    Console.WriteLine($"Order #{order.Id} failed: {ex.Message}");

                    order.Status = OrderStatus.Failed;
                    order.FailureReason = ex.Message;

                    // Release the stock reserved for this order.
                    foreach (OrderItem item in order.Items)
                    {
                        await _raceconditiondemo.ReleaseStockAsync(
                            item.ProductId,
                            item.Quantity,
                            ct);
                    }

                    await _orderrepository.SaveAsync(order, ct);
                }
            }

            // Step 10: Print the final status of every task.
            Console.WriteLine("\n--- Final Task Statuses ---");

            for (int i = 0; i < tasks.Count; i++)
            {
                Console.WriteLine($"Task[{i}] Status: {tasks[i].Status}");
            }
            stopwatch.Stop();
            // Step 11: Print total elapsed time.
            // Start the Stopwatch before processing begins.
            Console.WriteLine($"Total elapsed time: {stopwatch.ElapsedMilliseconds} ms");

            // Step 12: Read orders.json and verify the record count.
            List<Order> savedOrders =
                await _store.ReadAsync<Order>(DataFiles.Orders, ct);

            Console.WriteLine($"Orders saved: {savedOrders.Count}");

            if (savedOrders.Count == 20)
            {
                Console.WriteLine("Success: All 20 orders are saved.");
            }
            else
            {
                Console.WriteLine(
                    $"Warning: Expected 20 orders, but found {savedOrders.Count}.");
            }
         
            // Generate the final report.
            Task<Report> reportTask = _reprtgenerator.GenerateAsync(ct);

            Console.WriteLine("Generating report...");

            Report report = await reportTask;

            // Print the report.
            Console.WriteLine("\n--- Final Report ---");
            Console.WriteLine($"Total Orders: {report.TotalOrders}");
            Console.WriteLine($"Completed: {report.CompletedCount}");
            Console.WriteLine($"Rejected: {report.RejectedCount}");
            Console.WriteLine($"Failed: {report.FailedCount}");
            Console.WriteLine($"Payment Failed: {report.PaymentFailedCount}");
            Console.WriteLine($"Total Revenue: {report.TotalRevenue:F2}");
            Console.WriteLine($"Average Order Value: {report.AverageOrderValue:F2}");
            Console.WriteLine(
                $"Top Selling Product: {report.TopSellingProduct} ({report.TopSellingQuantity} units)");


        }
    }
}
        
    
