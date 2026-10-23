using OrderProcessing.Phase1;
using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Phase2;
using OrderProcessing.Phase3;
using OrderProcessing.Shared;
using System.Diagnostics;

public class Program
{

    public static async Task Main()
    {

        //var cts = new CancellationTokenSource();
        //var ct = cts.Token;

        //Phase3Runner p = new Phase3Runner();
        //await Phase3Runner.RunComparisonAsync(ct);
        var store = new JsonFileStore();

        OrderProcessing.Phase3.OrderRepository orderrepaository = new OrderProcessing.Phase3.OrderRepository(store, enableFailures: false);
        OrderProcessing.Phase3.EmailService email = new OrderProcessing.Phase3.EmailService(store, enableFailures: false);
        var customer = new Customer
        {
            Id = 1,
            Name = "Abdelrahman",
            Email = "abdelrahman@example.com",
            IsActive = true,
            LoyaltyTier = LoyaltyTier.Gold
        };

        var order = new Order
        {
            Id = 100,
            CustomerId = customer.Id,
            Items = new List<OrderItem>(),
            SubTotal = 1000m,
            Discount = 0m,
            ShippingCost = 50m,
            TotalAmount = 1050m,
            Status = OrderStatus.Validated,
            CreatedAt = DateTime.Now
        };

        await ProcessOrderAsync(order, customer.Email, orderrepaository, email);
    }


        public static  async Task ProcessOrderAsync(Order order, string customer, OrderProcessing.Phase3.OrderRepository orderrepaository, OrderProcessing.Phase3.EmailService email)
        {
        await orderrepaository.SaveAsync(order);
         await email.SendOrderConfirmationAsync(order, customer);   // not awaited
        Console.WriteLine("Order processing finished");
        }



}





