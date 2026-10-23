using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Phase3;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase4
{
    public class CustomerDashboardService
    {
        private readonly JsonFileStore _store;
        private readonly HashSet<int> _failPaymentsForCustomerIds;

        public CustomerDashboardService(
            JsonFileStore store,
            IEnumerable<int>? failPaymentsForCustomerIds = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _failPaymentsForCustomerIds = failPaymentsForCustomerIds != null
                ? new HashSet<int>(failPaymentsForCustomerIds)
                : new HashSet<int>();
        }

        public CustomerDashboardService(IEnumerable<int>? failPaymentsForCustomerIds = null)
            : this(new JsonFileStore(), failPaymentsForCustomerIds)
        {
        }

        public async Task<CustomerDashboard> LoadAsync(int customerId, CancellationToken ct = default)
        {
            // R1 & R2: Start all four loads at the same time
            var profileTask = LoadProfileAsync(customerId, ct);
            var ordersTask = LoadOrdersAsync(customerId, ct);
            var paymentsTask = LoadPaymentsAsync(customerId, ct);
            var emailsTask = LoadEmailsAsync(customerId, ct);

            var allTasks = Task.WhenAll(profileTask, ordersTask, paymentsTask, emailsTask);

            try
            {
                // Single await for all four concurrent loads
                await allTasks;
            }
            catch
            {
                // R4: If more than one load fails, report EVERY failure from allTasks.Exception
                if (allTasks.Exception != null && allTasks.Exception.InnerExceptions.Count > 1)
                {
                    throw new AggregateException(allTasks.Exception.InnerExceptions);
                }
                throw;
            }

            return new CustomerDashboard
            {
                Customer = await profileTask,
                Orders = await ordersTask,
                Payments = await paymentsTask,
                Emails = await emailsTask
            };
        }

        private async Task<Customer> LoadProfileAsync(int customerId, CancellationToken ct)
        {
            await SimulatedLatency.StepAsync(nameof(CustomerDashboardService), "Loading profile...", 300, 600, ct);
            var customers = await _store.ReadAsync<Customer>(DataFiles.Customers, ct);
            var customer = customers.FirstOrDefault(c => c.Id == customerId);
            return customer ?? throw new CustomerNotFoundException($"Customer #{customerId} not found");
        }

        private async Task<List<Order>> LoadOrdersAsync(int customerId, CancellationToken ct)
        {
            await SimulatedLatency.StepAsync(nameof(CustomerDashboardService), "Loading orders...", 300, 600, ct);
            var orders = await _store.ReadAsync<Order>(DataFiles.Orders, ct);
            return orders.Where(o => o.CustomerId == customerId).ToList();
        }

        private async Task<List<PaymentTransaction>> LoadPaymentsAsync(int customerId, CancellationToken ct)
        {
            await SimulatedLatency.StepAsync(nameof(CustomerDashboardService), "Loading payments...", 300, 600, ct);

            // R5: Failure injection for payments store timeout
            if (_failPaymentsForCustomerIds.Contains(customerId))
            {
                throw new TimeoutException("Payments store timeout");
            }

            // Find all order IDs for this customer to retrieve their payments
            var orders = await _store.ReadAsync<Order>(DataFiles.Orders, ct);
            var customerOrderIds = orders.Where(o => o.CustomerId == customerId).Select(o => o.Id).ToHashSet();

            var payments = await _store.ReadAsync<PaymentTransaction>(DataFiles.Payments, ct);
            return payments.Where(p => customerOrderIds.Contains(p.OrderId)).ToList();
        }

        private async Task<List<EmailMessage>> LoadEmailsAsync(int customerId, CancellationToken ct)
        {
            await SimulatedLatency.StepAsync(nameof(CustomerDashboardService), "Loading emails...", 300, 600, ct);

            var customers = await _store.ReadAsync<Customer>(DataFiles.Customers, ct);
            var customer = customers.FirstOrDefault(c => c.Id == customerId);
            string emailAddress = customer?.Email ?? string.Empty;

            var emails = await _store.ReadAsync<EmailMessage>(DataFiles.Emails, ct);
            return emails.Where(e => !string.IsNullOrEmpty(emailAddress) && e.To.Equals(emailAddress, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}
