using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class PricingService
    {
        private readonly JsonFileStore _store;
        private readonly CustomerRepository _coutomerRepository;
        private readonly CouponRepository _couponRepository;
        private readonly HashSet<int> _failShippingForOrderIds;

        public PricingService(JsonFileStore store,CustomerRepository c,CouponRepository co, IEnumerable<int> failShippingForOrderIds)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            if (c == null)
            {
                throw new ArgumentNullException(nameof(c));
            }
            if (co == null)
            {
                throw new ArgumentNullException(nameof(co));
            }
            if (failShippingForOrderIds == null)
            { throw new ArgumentNullException(nameof(failShippingForOrderIds)); }
            _store = store;
            _coutomerRepository = c;
            _couponRepository = co;
            _failShippingForOrderIds = new HashSet<int>(failShippingForOrderIds);
        }
        public async Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken ct)
        {
            await SimulatedLatency.StepAsync(
                nameof(PricingService),
                "Opening connection...",
                200,
                ct);

            await SimulatedLatency.StepAsync(
                nameof(PricingService),
                "Executing query...",
                300,
                600,
                ct);
            List<Product> products = await _store.ReadAsync<Product>(
                DataFiles.Products, ct);

            decimal subTotal = 0m;

            foreach (OrderItem item in order.Items)
            {
                Product product = products.First(p => p.Id == item.ProductId);

                item.UnitPrice = product.Price;

                subTotal += item.UnitPrice * item.Quantity;
            }

            order.SubTotal = Math.Round(
                subTotal, 2, MidpointRounding.AwayFromZero);

            return order.SubTotal;
        }
        public async Task<decimal> GetDiscountAsync(Order order, CancellationToken ct)
        {
            await SimulatedLatency.StepAsync(
                 nameof(PricingService),
                 "DNS + Handshake...",
                 150,
                 ct);

            await SimulatedLatency.StepAsync(
                nameof(PricingService),
                "Sending request...",
                600,
                ct);

            await SimulatedLatency.StepAsync(
                nameof(PricingService),
                "Waiting for response...",
                500,
                1500,
                ct);
            Customer? customer = await _coutomerRepository.GetByIdAsync(
                order.CustomerId, ct);

            if (customer is null)
            {
                throw new InvalidOperationException(
                    $"Customer #{order.CustomerId} not found");
            }

            decimal discountPercent = customer.LoyaltyTier switch
            {
                LoyaltyTier.Bronze => 0m,
                LoyaltyTier.Silver => 5m,
                LoyaltyTier.Gold => 10m,
                _ => 0m
            };

            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                Coupon? coupon = await _couponRepository.GetByCodeAsync(
                    order.CouponCode, ct);

                if (coupon is not null && coupon.ExpiryDate >= DateTime.Now)
                {
                    discountPercent += coupon.DiscountPercent;
                }
            }

            discountPercent = Math.Min(discountPercent, 30m);

            order.Discount = Math.Round(
                order.SubTotal * discountPercent / 100m,
                2,
                MidpointRounding.AwayFromZero);

            return order.Discount;
        }

       
        public async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken ct)
        {
            await SimulatedLatency.StepAsync(
                nameof(PricingService),
                "DNS + Handshake...",
                150,
                ct);

            await SimulatedLatency.StepAsync(
                nameof(PricingService),
                "Sending request...",
                600,
                ct);
            if (_failShippingForOrderIds.Contains(order.Id)) 
            { throw new ShippingProviderUnavailableException("Shipping provider unavailable"); }
            await SimulatedLatency.StepAsync(
                nameof(PricingService),
                "Waiting for response...",
                500,
                1500,
                ct);

            if (order.SubTotal >= 1000m)
            {
                order.ShippingCost = 0m;
                return order.ShippingCost;
            }

            List<Product> products =
                await _store.ReadAsync<Product>(DataFiles.Products, ct);

            int totalQuantity = 0;
            decimal totalWeight = 0m;

            foreach (OrderItem item in order.Items)
            {
                Product product = products.First(p => p.Id == item.ProductId);

                item.UnitPrice = product.Price;

                totalQuantity += item.Quantity;
                totalWeight += product.WeightKg * item.Quantity;
            }

            decimal shippingCost = 5.00m
                                 + 1.50m * totalQuantity
                                 + 2.00m * totalWeight;

            order.ShippingCost = Math.Round(
                shippingCost,
                2,
                MidpointRounding.AwayFromZero);

            return order.ShippingCost;
        }


    }
   
    public class ShippingProviderUnavailableException : Exception
    {
        public ShippingProviderUnavailableException(string message)
            : base(message)
        {
        }
    }


}
