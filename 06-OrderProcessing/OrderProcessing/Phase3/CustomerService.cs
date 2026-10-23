using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase3
{
    public class CustomerService
    {
        private readonly JsonFileStore _store;

        public CustomerService(JsonFileStore fileStore)
        {
            _store = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        }

        public CustomerService()
            : this(new JsonFileStore())
        {
        }

        public async Task<Customer> GetCustomerAsync(int id, CancellationToken ct = default)
        {
            await SimulatedLatency.StepAsync(nameof(CustomerService), "Opening Connection...", 200, ct);
            await SimulatedLatency.StepAsync(nameof(CustomerService), "Executing Query...", 300, 600, ct);

            var customers = await _store.ReadAsync<Customer>(DataFiles.Customers, ct);
            return customers.FirstOrDefault(c => c.Id == id)
                ?? throw new CustomerNotFoundException($"Customer #{id} not found");
        }

        public Customer GetCustomer(int id)
        {
            Log.Info(nameof(CustomerService), "Opening Connection...");
            Thread.Sleep(200);

            Log.Info(nameof(CustomerService), "Executing Query...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var customers = _store.Read<Customer>(DataFiles.Customers);
            return customers.FirstOrDefault(c => c.Id == id)
                ?? throw new CustomerNotFoundException($"Customer #{id} not found");
        }
    }

    public class CustomerNotFoundException : Exception
    {
        public CustomerNotFoundException(string message) : base(message)
        {
        }

        public CustomerNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
