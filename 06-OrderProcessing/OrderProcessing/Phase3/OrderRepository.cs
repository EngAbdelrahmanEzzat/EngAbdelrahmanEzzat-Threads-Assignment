using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase3
{
    public class OrderRepository
    {
        private readonly JsonFileStore _store;
        private readonly HashSet<int> _failSaveForOrderIds;
        private readonly bool _enableFailures;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public OrderRepository(
            JsonFileStore store,
            IEnumerable<int>? failSaveForOrderIds = null,
            bool enableFailures = true)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _enableFailures = enableFailures;
            _failSaveForOrderIds = failSaveForOrderIds != null ? new HashSet<int>(failSaveForOrderIds) : new HashSet<int>();
        }

        public OrderRepository(
            IEnumerable<int>? failSaveForOrderIds = null,
            bool enableFailures = true)
            : this(new JsonFileStore(), failSaveForOrderIds, enableFailures)
        {
        }

        // ==================== Asynchronous Methods ====================

        public async Task SaveAsync(Order order, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(order);

            await _semaphore.WaitAsync(ct);
            try
            {
                await SimulatedLatency.StepAsync(nameof(OrderRepository), "Opening connection...", 200, ct);
                await SimulatedLatency.StepAsync(nameof(OrderRepository), "Beginning transaction...", 100, ct);

                List<Order> orders = await _store.ReadAsync<Order>(DataFiles.Orders, ct);

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

                await SimulatedLatency.StepAsync(nameof(OrderRepository), "Inserting/Updating order...", 400, ct);

                // #13 DB save fails after payment
                if (_enableFailures && _failSaveForOrderIds.Contains(order.Id) && order.Status == OrderStatus.Completed)
                {
                    throw new DatabaseWriteException("Connection lost during commit");
                }

                await _store.WriteAsync<Order>(DataFiles.Orders, orders, ct);
                await SimulatedLatency.StepAsync(nameof(OrderRepository), "Committing transaction...", 200, ct);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
        {
            await SimulatedLatency.StepAsync(nameof(OrderRepository), "Opening connection...", 200, ct);
            await SimulatedLatency.StepAsync(nameof(OrderRepository), "Executing query...", 300, 600, ct);

            return await _store.ReadAsync<Order>(DataFiles.Orders, ct);
        }

        public async Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var orders = await GetAllAsync(ct);
            return orders.FirstOrDefault(o => o.Id == id);
        }

        public async Task<List<Order>> GetByStatusAsync(OrderStatus status, CancellationToken ct = default)
        {
            var orders = await GetAllAsync(ct);
            return orders.Where(o => o.Status == status).ToList();
        }

        // ==================== Synchronous Methods ====================

        public void Save(Order order)
        {
            ArgumentNullException.ThrowIfNull(order);

            _semaphore.Wait();
            try
            {
                Log.Info(nameof(OrderRepository), "Opening connection...");
                Thread.Sleep(200);

                Log.Info(nameof(OrderRepository), "Beginning transaction...");
                Thread.Sleep(100);

                List<Order> orders = _store.Read<Order>(DataFiles.Orders);

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

                Log.Info(nameof(OrderRepository), "Inserting/Updating order...");
                Thread.Sleep(400);

                if (_enableFailures && _failSaveForOrderIds.Contains(order.Id) && order.Status == OrderStatus.Completed)
                {
                    throw new DatabaseWriteException("Connection lost during commit");
                }

                _store.Write(DataFiles.Orders, orders);

                Log.Info(nameof(OrderRepository), "Committing transaction...");
                Thread.Sleep(200);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public List<Order> GetAll()
        {
            Log.Info(nameof(OrderRepository), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(OrderRepository), "Executing query...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            return _store.Read<Order>(DataFiles.Orders);
        }

        public Order? GetById(int id)
        {
            var orders = GetAll();
            return orders.FirstOrDefault(o => o.Id == id);
        }

        public List<Order> GetByStatus(OrderStatus status)
        {
            var orders = GetAll();
            return orders.Where(o => o.Status == status).ToList();
        }
    }

    public class DatabaseWriteException : Exception
    {
        public DatabaseWriteException(string message) : base(message)
        {
        }

        public DatabaseWriteException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
