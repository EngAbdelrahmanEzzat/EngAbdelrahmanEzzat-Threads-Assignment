using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase1.Repositories
{
    public class SyncCustomerRepository
    {
        private readonly JsonFileStore _store;

        public SyncCustomerRepository(JsonFileStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public Customer? GetById(int id)
        {
            Log.Info(nameof(SyncCustomerRepository), "Opening connection...");
            Thread.Sleep(200);

            int delay = Random.Shared.Next(300, 601);
            Log.Info(nameof(SyncCustomerRepository), "Executing query...");
            Thread.Sleep(delay);

            List<Customer> customers = _store.Read<Customer>(
                DataFiles.Customers);

            return customers.FirstOrDefault(c => c.Id == id);
        }
    }
}