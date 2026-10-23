using System;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Phase2;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase3
{
    public class OrderProcessor
    {
        private readonly CustomerService _customerService;
        private readonly InventoryService _inventoryService;
        private readonly PricingService _pricingService;
        private readonly IPaymentGateway _paymentGateway;
        private readonly OrderRepository _orderRepository;
        private readonly EmailService _emailService;

        public OrderProcessor(
            CustomerService customerService,
            InventoryService inventoryService,
            PricingService pricingService,
            IPaymentGateway paymentGateway,
            OrderRepository orderRepository,
            EmailService emailService)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _pricingService = pricingService ?? throw new ArgumentNullException(nameof(pricingService));
            _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        // ==================== Asynchronous Processing ====================

        public async Task<bool> ProcessOrderAsync(Order order, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(order);

            Customer customer;
            try
            {
                customer = await _customerService.GetCustomerAsync(order.CustomerId, ct);
            }
            catch (CustomerNotFoundException ex)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;
                await _orderRepository.SaveAsync(order, ct);
                return false;
            }

            if (!customer.IsActive)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = $"Customer #{customer.Id} is inactive.";
                await _orderRepository.SaveAsync(order, ct);
                return false;
            }

            bool reserved = await _inventoryService.ReserveStockAsync(order, ct);
            if (!reserved)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = "Insufficient stock";
                await _orderRepository.SaveAsync(order, ct);
                return false;
            }

            try
            {
                await _pricingService.CalculateTotalAsync(order, ct);
            }
            catch (Exception ex)
            {
                await _inventoryService.ReleaseStockAsync(order, ct);
                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;
                await _orderRepository.SaveAsync(order, ct);
                return false;
            }

            PaymentResult paymentResult;
            try
            {
                paymentResult = await _paymentGateway.ProcessPaymentAsync(order, ct);
            }
            catch (Exception ex)
            {
                await _inventoryService.ReleaseStockAsync(order, ct);
                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;
                await _orderRepository.SaveAsync(order, ct);
                return false;
            }

            if (!paymentResult.Success)
            {
                await _inventoryService.ReleaseStockAsync(order, ct);
                order.Status = paymentResult.Status == PaymentStatus.Declined
                    ? OrderStatus.PaymentFailed
                    : OrderStatus.Failed;
                order.FailureReason = paymentResult.Message;
                await _orderRepository.SaveAsync(order, ct);
                return false;
            }

            order.Status = OrderStatus.Completed;
            order.FailureReason = null;
            order.TransactionId = paymentResult.TransactionId;

            try
            {
                await _orderRepository.SaveAsync(order, ct);
            }
            catch (DatabaseWriteException ex)
            {
                await _inventoryService.ReleaseStockAsync(order, ct);
                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;
                return false;
            }

            try
            {
                await _emailService.SendOrderConfirmationAsync(order, customer.Email, ct);
            }
            catch (SmtpUnavailableException ex)
            {
                Log.Info(nameof(OrderProcessor), $"Email notification failed for Order #{order.Id}: {ex.Message}");
            }

            return true;
        }

        // ==================== Synchronous Processing ====================

        public bool ProcessOrder(Order order)
        {
            ArgumentNullException.ThrowIfNull(order);

            Customer customer;
            try
            {
                customer = _customerService.GetCustomer(order.CustomerId);
            }
            catch (CustomerNotFoundException ex)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;
                _orderRepository.Save(order);
                return false;
            }

            if (!customer.IsActive)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = $"Customer #{customer.Id} is inactive.";
                _orderRepository.Save(order);
                return false;
            }

            bool reserved = _inventoryService.ReserveStock(order);
            if (!reserved)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = "Insufficient stock";
                _orderRepository.Save(order);
                return false;
            }

            try
            {
                _pricingService.CalculateTotal(order);
            }
            catch (Exception ex)
            {
                _inventoryService.ReleaseStock(order);
                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;
                _orderRepository.Save(order);
                return false;
            }

            PaymentResult paymentResult;
            try
            {
                paymentResult = _paymentGateway.ProcessPayment(order);
            }
            catch (Exception ex)
            {
                _inventoryService.ReleaseStock(order);
                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;
                _orderRepository.Save(order);
                return false;
            }

            if (!paymentResult.Success)
            {
                _inventoryService.ReleaseStock(order);
                order.Status = paymentResult.Status == PaymentStatus.Declined
                    ? OrderStatus.PaymentFailed
                    : OrderStatus.Failed;
                order.FailureReason = paymentResult.Message;
                _orderRepository.Save(order);
                return false;
            }

            order.Status = OrderStatus.Completed;
            order.FailureReason = null;
            order.TransactionId = paymentResult.TransactionId;

            try
            {
                _orderRepository.Save(order);
            }
            catch (DatabaseWriteException ex)
            {
                _inventoryService.ReleaseStock(order);
                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;
                return false;
            }

            try
            {
                _emailService.SendOrderConfirmation(order, customer.Email);
            }
            catch (SmtpUnavailableException ex)
            {
                Log.Info(nameof(OrderProcessor), $"Email notification failed for Order #{order.Id}: {ex.Message}");
            }

            return true;
        }
    }
}
