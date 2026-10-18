using System;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared;
using OrderProcessing.Phase1.Repositories;

namespace OrderProcessing.Phase1
{
    public class OrderValidator
    {
        private readonly CustomerRepository _customerRepository;
        private readonly ProductRepository _productRepository;
        private readonly CouponRepository _couponRepository;
        private readonly OrderRepository _orderRepository;

        public OrderValidator(
            CustomerRepository customerRepository,
            ProductRepository productRepository,
            CouponRepository couponRepository,
            OrderRepository orderRepository)
        {
            _customerRepository = customerRepository
                ?? throw new ArgumentNullException(nameof(customerRepository));

            _productRepository = productRepository
                ?? throw new ArgumentNullException(nameof(productRepository));

            _couponRepository = couponRepository
                ?? throw new ArgumentNullException(nameof(couponRepository));

            _orderRepository = orderRepository
                ?? throw new ArgumentNullException(nameof(orderRepository));
        }

        public async Task<Order> ValidateOrderAsync(
            Order order,
            CancellationToken ct)
        { 
            // استخدمت الاتنين دول عشان لاسايمنت قايل لاوم تستخدم 2 تاخير 
            await SimulatedLatency.StepAsync(
                nameof(OrderValidator),
                "Starting order validation...",
                100,
                ct);

            await SimulatedLatency.StepAsync(
                nameof(OrderValidator),
                "Checking order details...",
                100,
                ct);

            var customer = await _customerRepository.GetByIdAsync(
                order.CustomerId,
                ct);

            if (customer == null)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = $"Customer {order.CustomerId} not found.";
                await _orderRepository.SaveAsync(order, ct);
                return order;
            }

            if (!customer.IsActive)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = $"Customer {order.CustomerId} is inactive.";
                await _orderRepository.SaveAsync(order, ct);
                return order;
            }

            foreach (var item in order.Items)
            {
                var product = await _productRepository.GetByIdAsync(
                    item.ProductId,
                    ct);

                if (product == null)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Product {item.ProductId} not found.";
                    await _orderRepository.SaveAsync(order, ct);
                    return order;
                }

                if (product.StockQuantity < item.Quantity)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason =
                        $"Insufficient stock for product {product.Name}.";
                    await _orderRepository.SaveAsync(order, ct);
                    return order;
                }
            }

            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                var coupon = await _couponRepository.GetByCodeAsync(
                    order.CouponCode,
                    ct);

                if (coupon == null)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason =
                        $"Coupon {order.CouponCode} not found.";
                    await _orderRepository.SaveAsync(order, ct);
                    return order;
                }

                if (coupon.ExpiryDate.Date < DateTime.Today)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason =
                        $"Coupon {order.CouponCode} has expired.";
                    await _orderRepository.SaveAsync(order, ct);
                    return order;
                }
            }

            order.Status = OrderStatus.Validated;
            order.FailureReason = null;

            await _orderRepository.SaveAsync(order, ct);

            return order;
        }
    }
}