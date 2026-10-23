using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase3
{
    public class PaymentGateway : IPaymentGateway
    {
        private readonly JsonFileStore _store;
        private readonly HashSet<int> _timeoutOrderIds;
        private readonly HashSet<int> _declineOrderIds;
        private readonly bool _enableFailures;
        private static readonly SemaphoreSlim _paymentSemaphore = new(1, 1);

        public PaymentGateway(
            JsonFileStore store,
            IEnumerable<int>? timeoutOrderIds = null,
            IEnumerable<int>? declineOrderIds = null,
            bool enableFailures = true)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _enableFailures = enableFailures;
            _timeoutOrderIds = timeoutOrderIds != null ? new HashSet<int>(timeoutOrderIds) : new HashSet<int>();
            _declineOrderIds = declineOrderIds != null ? new HashSet<int>(declineOrderIds) : new HashSet<int>();
        }

        public PaymentGateway(
            IEnumerable<int>? timeoutOrderIds = null,
            IEnumerable<int>? declineOrderIds = null,
            bool enableFailures = true)
            : this(new JsonFileStore(), timeoutOrderIds, declineOrderIds, enableFailures)
        {
        }

        public async Task<PaymentResult> ProcessPaymentAsync(Order order, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(order);

            // Step 1: Fraud check
            await SimulatedLatency.StepAsync(nameof(PaymentGateway), "Running fraud check...", 200, ct);

            // #8 Payment declined: after Fraud check: Success=false, Declined, "Card declined by issuer"
            if (_enableFailures && _declineOrderIds.Contains(order.Id))
            {
                var declinedResult = new PaymentResult
                {
                    Success = false,
                    Status = PaymentStatus.Declined,
                    Message = "Card declined by issuer"
                };

                await RecordTransactionAsync(order.Id, order.TotalAmount, PaymentStatus.Declined, null, ct);
                return declinedResult;
            }

            // Step 2: Capture payment
            // #5 Payment timeout: Capture uses Task.Delay(5000, ct) instead of 700
            if (_enableFailures && _timeoutOrderIds.Contains(order.Id))
            {
                await SimulatedLatency.StepAsync(nameof(PaymentGateway), "Capturing payment with high latency...", 5000, ct);
            }
            else
            {
                await SimulatedLatency.StepAsync(nameof(PaymentGateway), "Capturing payment...", 700, ct);
            }

            string txnId = $"TXN-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

            await RecordTransactionAsync(order.Id, order.TotalAmount, PaymentStatus.Captured, txnId, ct);

            return new PaymentResult
            {
                Success = true,
                TransactionId = txnId,
                Status = PaymentStatus.Captured,
                Message = "Payment captured successfully"
            };
        }

        public PaymentResult ProcessPayment(Order order)
        {
            ArgumentNullException.ThrowIfNull(order);

            // Step 1: Fraud check
            Log.Info(nameof(PaymentGateway), "Running fraud check...");
            Thread.Sleep(200);

            if (_enableFailures && _declineOrderIds.Contains(order.Id))
            {
                var declinedResult = new PaymentResult
                {
                    Success = false,
                    Status = PaymentStatus.Declined,
                    Message = "Card declined by issuer"
                };

                RecordTransaction(order.Id, order.TotalAmount, PaymentStatus.Declined, null);
                return declinedResult;
            }

            // Step 2: Capture payment
            if (_enableFailures && _timeoutOrderIds.Contains(order.Id))
            {
                Log.Info(nameof(PaymentGateway), "Capturing payment with high latency...");
                Thread.Sleep(5000);
            }
            else
            {
                Log.Info(nameof(PaymentGateway), "Capturing payment...");
                Thread.Sleep(700);
            }

            string txnId = $"TXN-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

            RecordTransaction(order.Id, order.TotalAmount, PaymentStatus.Captured, txnId);

            return new PaymentResult
            {
                Success = true,
                TransactionId = txnId,
                Status = PaymentStatus.Captured,
                Message = "Payment captured successfully"
            };
        }

        private async Task RecordTransactionAsync(
            int orderId,
            decimal amount,
            PaymentStatus status,
            string? transactionId,
            CancellationToken ct)
        {
            await _paymentSemaphore.WaitAsync(ct);
            try
            {
                var transactions = await _store.ReadAsync<PaymentTransaction>(DataFiles.Payments, ct);
                transactions.Add(new PaymentTransaction
                {
                    OrderId = orderId,
                    Amount = amount,
                    Status = status,
                    TransactionId = transactionId,
                    CreatedAt = DateTime.Now
                });
                await _store.WriteAsync(DataFiles.Payments, transactions, ct);
            }
            finally
            {
                _paymentSemaphore.Release();
            }
        }

        private void RecordTransaction(
            int orderId,
            decimal amount,
            PaymentStatus status,
            string? transactionId)
        {
            _paymentSemaphore.Wait();
            try
            {
                var transactions = _store.Read<PaymentTransaction>(DataFiles.Payments);
                transactions.Add(new PaymentTransaction
                {
                    OrderId = orderId,
                    Amount = amount,
                    Status = status,
                    TransactionId = transactionId,
                    CreatedAt = DateTime.Now
                });
                _store.Write(DataFiles.Payments, transactions);
            }
            finally
            {
                _paymentSemaphore.Release();
            }
        }
    }
}
