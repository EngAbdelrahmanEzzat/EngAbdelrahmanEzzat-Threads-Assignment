using System;
using System.Collections.Generic;
using System.Text;
using OrderProcessing;
using OrderProcessing.Shared;
namespace OrderProcessing.Phase1.Repositories
{

    public class OrderRepository
    {
        private readonly JsonFileStore _store;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);


        public OrderRepository(JsonFileStore store)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            _store = store;
        }

        public async Task SaveAsync(Order order, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                await SimulatedLatency.StepAsync(
                    nameof(OrderRepository),
                    "Opening connection...",
                    200,
                    ct);

                await SimulatedLatency.StepAsync(
                    nameof(OrderRepository),
                    "Beginning transaction...",
                    100,
                    ct);

                List<Order> orders = await _store.ReadAsync<Order>(
                    DataFiles.Orders,
                    ct);

                bool orderExists = false;

                for (int i = 0; i < orders.Count; i++)
                {
                    if (orders[i].Id == order.Id)
                    {
                        orders[i] = order;
                        orderExists = true;
                        break;
                    }
                }

                if (!orderExists)
                {
                    orders.Add(order);
                }

                await SimulatedLatency.StepAsync(
                    nameof(OrderRepository),
                    "Inserting/Updating order...",
                    400,
                    ct);

                await _store.WriteAsync<Order>(
                    DataFiles.Orders,
                    orders,
                    ct);

                await SimulatedLatency.StepAsync(
                    nameof(OrderRepository),
                    "Committing transaction...",
                    200,
                    ct);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<Order>> GetAllAsync(CancellationToken ct)
        {
            await SimulatedLatency.StepAsync(
                nameof(OrderRepository),
                "Opening connection...",
                200,
                ct);

            await SimulatedLatency.StepAsync(
                nameof(OrderRepository),
                "Executing query...",
                300,
                600,
                ct);

            return await _store.ReadAsync<Order>(
                DataFiles.Orders,
                ct);
        }
        public async Task<List<Order>> GetByStatusAsync( OrderStatus status, CancellationToken ct)
        {
            await SimulatedLatency.StepAsync(
                nameof(OrderRepository),
                "Opening connection...",
                200,
                ct);

            await SimulatedLatency.StepAsync(
                nameof(OrderRepository),
                "Executing query...",
                300,
                600,
                ct);

            List<Order> orders = await _store.ReadAsync<Order>(
                DataFiles.Orders,
                ct);

            return orders
                .Where(order => order.Status == status)
                .ToList();
        }
    }



}
