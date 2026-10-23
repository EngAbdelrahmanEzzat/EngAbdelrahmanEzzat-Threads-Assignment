using System;
using System.Collections.Generic;
using System.Text;
using OrderProcessing;
using OrderProcessing.Shared;
   
    namespace OrderProcessing.Phase1.Repositories
    {
        public class IncomingOrderRepository
        {
            private readonly JsonFileStore _store;

            public IncomingOrderRepository(JsonFileStore store)
            {
                if (store == null)
                {
                    throw new ArgumentNullException(nameof(store));
                }

                _store = store;
            }

            public async Task<List<Order>> GetAllAsync(CancellationToken ct)
            {
                await SimulatedLatency.StepAsync(
                    nameof(IncomingOrderRepository),
                    "Opening connection...",
                    200,
                    ct);

                await SimulatedLatency.StepAsync(
                    nameof(IncomingOrderRepository),
                    "Executing query...",
                    300,
                    600,
                    ct);

                return await _store.ReadAsync<Order>(
                    DataFiles.IncomingOrders,
                    ct);
            }
           public List<Order> GetAll()
           {
            Log.Info(nameof(IncomingOrderRepository), "Opening connection.....");
            Thread.Sleep(200);
            Log.Info(nameof(IncomingOrderRepository), "Excuting query....");
            Thread.Sleep(Random.Shared.Next(300, 601));
            return _store.Read<Order>(DataFiles.IncomingOrders);
           }
    }
    }

