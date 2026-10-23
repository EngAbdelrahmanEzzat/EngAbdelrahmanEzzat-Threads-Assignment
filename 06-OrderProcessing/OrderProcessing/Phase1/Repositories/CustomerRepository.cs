using OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing;
namespace OrderProcessing.Phase1.Repositories
{
    public class CustomerRepository
    {
        private readonly JsonFileStore _store;

        public CustomerRepository(JsonFileStore store)
        {
            if(store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            _store = store;
        }
        
        public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct)
        {
            await SimulatedLatency.StepAsync(nameof(CustomerRepository), "Opining Connection...",200, ct);
            await SimulatedLatency.StepAsync(nameof(CustomerRepository), "Executing Query...", 300, 600, ct);

            var customers = await _store.ReadAsync<Customer>(DataFiles.Customers, ct);
            return customers.FirstOrDefault(c => c.Id == id);
        }
    }
}
