using System;
using System.Collections.Generic;
using OrderProcessing.Shared;
using OrderProcessing.Phase1.Repositories;

namespace OrderProcessing.Phase1
{
    public class SyncOrderValidator
    {
        private readonly SyncCustomerRepository _customerRepository;
        private readonly SyncProductRepository _productRepository;
        private readonly SyncCouponRepository _couponRepository;

        public SyncOrderValidator(
            SyncCustomerRepository customerRepository,
            SyncProductRepository productRepository,
            SyncCouponRepository couponRepository)
        {
            _customerRepository = customerRepository
                ?? throw new ArgumentNullException(nameof(customerRepository));

            _productRepository = productRepository
                ?? throw new ArgumentNullException(nameof(productRepository));

            _couponRepository = couponRepository
                ?? throw new ArgumentNullException(nameof(couponRepository));
        }

        public Order ValidateOrder(Order order)
        {
            Log.Info(nameof(SyncOrderValidator), "Starting order validation...");
           Thread.Sleep(100);

            Log.Info(nameof(SyncOrderValidator), "Checking order details...");
            Thread.Sleep(100);

            var customer = _customerRepository.GetById(
                order.CustomerId
                );

            if (customer == null)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = $"Customer {order.CustomerId} not found.";

                return order;
            }

            if (!customer.IsActive)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = $"Customer {order.CustomerId} is inactive.";

                return order;
            }

            foreach (var item in order.Items)
            {
                var product = _productRepository.GetById(
                    item.ProductId
                    );

                if (product == null)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Product {item.ProductId} not found.";

                    return order;
                }

                if (product.StockQuantity < item.Quantity)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason =
                        $"Insufficient stock for product {product.Name}.";

                    return order;
                }
            }

            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                var coupon = _couponRepository.GetByCode(
                    order.CouponCode
                    );

                if (coupon == null)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason =
                        $"Coupon {order.CouponCode} not found.";

                    return order;
                }

                if (coupon.ExpiryDate.Date < DateTime.Today)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason =
                        $"Coupon {order.CouponCode} has expired.";

                    return order;
                }
            }

            order.Status = OrderStatus.Validated;
            order.FailureReason = null;



            return order;
        }    }
}