using OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace OrderProcessing.Phase2
{
    public class OrderPricer
    {
        
        private readonly PricingService _pricingservice;

        public OrderPricer(PricingService pricingservice)
        {
            _pricingservice = pricingservice;
        }
        public async Task PriceOrderAsync(Order order,CancellationToken ct)
        {
            decimal subtotal= await _pricingservice.CalculateSubTotalAsync(order,ct);
            var discount = _pricingservice.GetDiscountAsync(order, ct);
            var shippig = _pricingservice.CalculateShippingCostAsync(order, ct);
            await Task.WhenAll(discount, shippig);

            order.Discount = await discount;
            order.ShippingCost = await shippig;

            decimal Total = order.SubTotal - order.Discount + order.ShippingCost;

            order.TotalAmount = Math.Round(Total, 2, MidpointRounding.AwayFromZero);

        }
        public async Task PriceOrderSequentialAsync(Order order,CancellationToken ct)
        {
            var subtotal = await _pricingservice.CalculateSubTotalAsync(order, ct);
            var discount = await _pricingservice.GetDiscountAsync(order, ct);
            var shipping = await _pricingservice.CalculateShippingCostAsync(order, ct);

            order.Discount = discount;
            order.ShippingCost = shipping;

            var Total = subtotal - order.Discount + order.ShippingCost;

        }
    }
    
    
}
