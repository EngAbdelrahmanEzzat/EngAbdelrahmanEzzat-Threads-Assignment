using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Phase2;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase3
{
    public class PricingService
    {
        private readonly JsonFileStore _store;
        private readonly HashSet<int> _failShippingForOrderIds;

        public PricingService(JsonFileStore store, IEnumerable<int>? failShippingForOrderIds = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _failShippingForOrderIds = failShippingForOrderIds != null
                ? new HashSet<int>(failShippingForOrderIds)
                : new HashSet<int>();
        }

        public PricingService(IEnumerable<int>? failShippingForOrderIds = null)
            : this(new JsonFileStore(), failShippingForOrderIds)
        {
        }

        // ==================== Asynchronous Method ====================

        public async Task<decimal> CalculateTotalAsync(Order order, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(order);

            // 1. Coupon validation
            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                var coupons = await _store.ReadAsync<Coupon>(DataFiles.Coupons, ct);
                var coupon = coupons.FirstOrDefault(c => c.Code.Equals(order.CouponCode, StringComparison.OrdinalIgnoreCase));

                if (coupon is null || coupon.ExpiryDate < DateTime.Now)
                {
                    throw new InvalidCouponException("Coupon is not valid");
                }
            }

            // 2. SubTotal calculation
            await SimulatedLatency.StepAsync(nameof(PricingService), "Opening connection...", 200, ct);
            await SimulatedLatency.StepAsync(nameof(PricingService), "Executing query...", 300, 600, ct);

            var products = await _store.ReadAsync<Product>(DataFiles.Products, ct);
            decimal subTotal = 0m;
            int totalQuantity = 0;
            decimal totalWeight = 0m;

            foreach (var item in order.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId)
                    ?? throw new InvalidOperationException($"Product #{item.ProductId} not found");

                item.UnitPrice = product.Price;
                subTotal += item.UnitPrice * item.Quantity;
                totalQuantity += item.Quantity;
                totalWeight += product.WeightKg * item.Quantity;
            }

            order.SubTotal = Math.Round(subTotal, 2, MidpointRounding.AwayFromZero);

            // 3. Discount calculation
            await SimulatedLatency.StepAsync(nameof(PricingService), "Checking customer loyalty discount...", 150, ct);
            var customers = await _store.ReadAsync<Customer>(DataFiles.Customers, ct);
            var customer = customers.FirstOrDefault(c => c.Id == order.CustomerId)
                ?? throw new InvalidOperationException($"Customer #{order.CustomerId} not found");

            decimal discountPercent = customer.LoyaltyTier switch
            {
                LoyaltyTier.Bronze => 0m,
                LoyaltyTier.Silver => 5m,
                LoyaltyTier.Gold => 10m,
                _ => 0m
            };

            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                var coupons = await _store.ReadAsync<Coupon>(DataFiles.Coupons, ct);
                var coupon = coupons.FirstOrDefault(c => c.Code.Equals(order.CouponCode, StringComparison.OrdinalIgnoreCase));
                if (coupon is not null && coupon.ExpiryDate >= DateTime.Now)
                {
                    discountPercent += coupon.DiscountPercent;
                }
            }

            discountPercent = Math.Min(discountPercent, 30m);
            order.Discount = Math.Round(order.SubTotal * discountPercent / 100m, 2, MidpointRounding.AwayFromZero);

            // 4. Shipping cost calculation
            await SimulatedLatency.StepAsync(nameof(PricingService), "Calculating shipping cost...", 200, ct);

            if (_failShippingForOrderIds.Contains(order.Id))
            {
                throw new ShippingProviderUnavailableException("Shipping provider unavailable");
            }

            if (order.SubTotal >= 1000m)
            {
                order.ShippingCost = 0m;
            }
            else
            {
                decimal shippingCost = 5.00m + 1.50m * totalQuantity + 2.00m * totalWeight;
                order.ShippingCost = Math.Round(shippingCost, 2, MidpointRounding.AwayFromZero);
            }

            // 5. Total
            order.TotalAmount = Math.Round(order.SubTotal - order.Discount + order.ShippingCost, 2, MidpointRounding.AwayFromZero);
            return order.TotalAmount;
        }

        // ==================== Synchronous Method ====================

        public decimal CalculateTotal(Order order)
        {
            ArgumentNullException.ThrowIfNull(order);

            // 1. Coupon validation
            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                var coupons = _store.Read<Coupon>(DataFiles.Coupons);
                var coupon = coupons.FirstOrDefault(c => c.Code.Equals(order.CouponCode, StringComparison.OrdinalIgnoreCase));

                if (coupon is null || coupon.ExpiryDate < DateTime.Now)
                {
                    throw new InvalidCouponException("Coupon is not valid");
                }
            }

            // 2. SubTotal calculation
            Log.Info(nameof(PricingService), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(PricingService), "Executing query...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            var products = _store.Read<Product>(DataFiles.Products);
            decimal subTotal = 0m;
            int totalQuantity = 0;
            decimal totalWeight = 0m;

            foreach (var item in order.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId)
                    ?? throw new InvalidOperationException($"Product #{item.ProductId} not found");

                item.UnitPrice = product.Price;
                subTotal += item.UnitPrice * item.Quantity;
                totalQuantity += item.Quantity;
                totalWeight += product.WeightKg * item.Quantity;
            }

            order.SubTotal = Math.Round(subTotal, 2, MidpointRounding.AwayFromZero);

            // 3. Discount calculation
            Log.Info(nameof(PricingService), "Checking customer loyalty discount...");
            Thread.Sleep(150);

            var customers = _store.Read<Customer>(DataFiles.Customers);
            var customer = customers.FirstOrDefault(c => c.Id == order.CustomerId)
                ?? throw new InvalidOperationException($"Customer #{order.CustomerId} not found");

            decimal discountPercent = customer.LoyaltyTier switch
            {
                LoyaltyTier.Bronze => 0m,
                LoyaltyTier.Silver => 5m,
                LoyaltyTier.Gold => 10m,
                _ => 0m
            };

            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                var coupons = _store.Read<Coupon>(DataFiles.Coupons);
                var coupon = coupons.FirstOrDefault(c => c.Code.Equals(order.CouponCode, StringComparison.OrdinalIgnoreCase));
                if (coupon is not null && coupon.ExpiryDate >= DateTime.Now)
                {
                    discountPercent += coupon.DiscountPercent;
                }
            }

            discountPercent = Math.Min(discountPercent, 30m);
            order.Discount = Math.Round(order.SubTotal * discountPercent / 100m, 2, MidpointRounding.AwayFromZero);

            // 4. Shipping cost calculation
            Log.Info(nameof(PricingService), "Calculating shipping cost...");
            Thread.Sleep(200);

            if (_failShippingForOrderIds.Contains(order.Id))
            {
                throw new ShippingProviderUnavailableException("Shipping provider unavailable");
            }

            if (order.SubTotal >= 1000m)
            {
                order.ShippingCost = 0m;
            }
            else
            {
                decimal shippingCost = 5.00m + 1.50m * totalQuantity + 2.00m * totalWeight;
                order.ShippingCost = Math.Round(shippingCost, 2, MidpointRounding.AwayFromZero);
            }

            // 5. Total
            order.TotalAmount = Math.Round(order.SubTotal - order.Discount + order.ShippingCost, 2, MidpointRounding.AwayFromZero);
            return order.TotalAmount;
        }
    }

    public class InvalidCouponException : Exception
    {
        public InvalidCouponException(string message) : base(message)
        {
        }

        public InvalidCouponException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
