using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class OrderProcessorV2
    {
        private readonly JsonFileStore _store; 
        private readonly RaceConditionDemo _raceConditionDemo;
        private readonly OrderPricer _orderPricer;
        
        private readonly OrderRepository _orderRepository;
        public OrderProcessorV2(
            JsonFileStore store, 
            RaceConditionDemo raceConditionDemo,
            OrderPricer orderPricer, 
          OrderRepository orderRepository)
        { _store = store ?? throw new ArgumentNullException(nameof(store));
            _raceConditionDemo = raceConditionDemo ?? throw new ArgumentNullException(nameof(raceConditionDemo));
            _orderPricer = orderPricer ?? throw new ArgumentNullException(nameof(orderPricer));
           
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository)); 
        }


        public async Task<bool> ProcessValidatedOrderAsync(
          Order order,
          CancellationToken ct = default)
        {
            List<(int ProductId, int Quantity)> reservedItems = new();

            List<Product> products =
                await _store.ReadAsync<Product>(DataFiles.Products, ct);

            foreach (OrderItem item in order.Items)
            {
                Product? product =
                    products.FirstOrDefault(p => p.Id == item.ProductId);

                if (product is null)
                {
                    throw new InvalidOperationException(
                        $"Product #{item.ProductId} not found.");
                }

                bool reserved = await _raceConditionDemo.ReserveStockAsync(
                    item.ProductId,
                    item.Quantity,
                    ct);

                if (!reserved)
                {
                    foreach (var reservedItem in reservedItems)
                    {
                        await _raceConditionDemo.ReleaseStockAsync(
                            reservedItem.ProductId,
                            reservedItem.Quantity,
                            ct);
                    }

                    order.Status = OrderStatus.Rejected;
                    order.FailureReason =
                        $"Insufficient stock for '{product.Name}'";

                    await _orderRepository.SaveAsync(order, ct);

                    return false;
                }

                reservedItems.Add((item.ProductId, item.Quantity));
            }

            await _orderPricer.PriceOrderAsync(order, ct);

            await InvoiceGenerator.GenerateAsync(order, ct);

            order.Status = OrderStatus.Completed;
            order.FailureReason = null;

            await _orderRepository.SaveAsync(order, ct);

            return true;
        }


    }
}
