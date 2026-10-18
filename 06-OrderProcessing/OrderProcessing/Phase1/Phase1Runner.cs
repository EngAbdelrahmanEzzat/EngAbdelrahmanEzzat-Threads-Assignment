using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase1
{
    public class Phase1Runner
    {
        private readonly JsonFileStore _store;

        private readonly IncomingOrderRepository _incomingOrderRepository;
        private readonly OrderRepository _orderRepository;

        private readonly SyncOrderValidator _syncOrderValidator;
        private readonly OrderValidator _orderValidator;

        public Phase1Runner(
            JsonFileStore store,
            IncomingOrderRepository incomingOrderRepository,
            OrderRepository orderRepository,
            SyncOrderValidator syncOrderValidator,
            OrderValidator orderValidator)
        {
            _store = store
                ?? throw new ArgumentNullException(nameof(store));

            _incomingOrderRepository = incomingOrderRepository
                ?? throw new ArgumentNullException(nameof(incomingOrderRepository));

            _orderRepository = orderRepository
                ?? throw new ArgumentNullException(nameof(orderRepository));

            _syncOrderValidator = syncOrderValidator
                ?? throw new ArgumentNullException(nameof(syncOrderValidator));

            _orderValidator = orderValidator
                ?? throw new ArgumentNullException(nameof(orderValidator));
        }

        private TimeSpan RunSync(List<Order> orders)
        {
            var stopwatch = Stopwatch.StartNew();

            foreach (Order order in orders)
            {
                _syncOrderValidator.ValidateOrder(order);
            }

            stopwatch.Stop();

            return stopwatch.Elapsed;
        }

        private async Task<TimeSpan> RunAsyncValidation(
            List<Order> orders,
            CancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();

            foreach (Order order in orders)
            {
                //الكود ده بيننفز الاتنين ال validate و save .
                await _orderValidator.ValidateOrderAsync(order, ct);
               
            }

            stopwatch.Stop();

            return stopwatch.Elapsed;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            // Reset before Sync run
            await DataSeeder.ResetAsync(ct);

            List<Order> syncOrders =
                _incomingOrderRepository.GetAll();

            TimeSpan syncTime = RunSync(syncOrders);

            // Reset before Async run
            await DataSeeder.ResetAsync(ct);

            List<Order> asyncOrders =
                await _incomingOrderRepository.GetAllAsync(ct);

            TimeSpan asyncTime =
                await RunAsyncValidation(asyncOrders, ct);

            // Read the summary from orders.json
            List<Order> savedOrders =
                await _orderRepository.GetAllAsync(ct);

            List<Order> validated =
                await _orderRepository.GetByStatusAsync(
                    OrderStatus.Validated, ct);

            List<Order> rejected =
                await _orderRepository.GetByStatusAsync(
                    OrderStatus.Rejected, ct);

            Console.WriteLine();
            Console.WriteLine("===== Validation Summary =====");
            Console.WriteLine($"Total Orders: {savedOrders.Count}");
            Console.WriteLine($"Validated:    {validated.Count}");
            Console.WriteLine($"Rejected:     {rejected.Count}");

            foreach (Order order in rejected)
            {
                Console.WriteLine(
                    $"  - Order #{order.Id}:  {order.FailureReason}");
            }

            Console.WriteLine(
                $"Sync Time:  {syncTime.TotalSeconds:F1} seconds");

            Console.WriteLine(
                $"Async Time: {asyncTime.TotalSeconds:F1} seconds");

            Console.WriteLine(
                $"orders.json now contains {savedOrders.Count} records.");
        }
    }
}