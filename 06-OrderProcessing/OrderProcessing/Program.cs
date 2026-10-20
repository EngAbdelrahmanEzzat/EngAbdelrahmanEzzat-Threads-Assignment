using OrderProcessing.Phase1;
using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Phase2;
using OrderProcessing.Shared;
using System.Diagnostics;

public class Program
{
    public static async Task Main()
    {
       


        //Phase 2
        // Create dependencies
        var store = new JsonFileStore();
        var customerRepository = new CustomerRepository(store);
        var couponRepository = new CouponRepository(store);
        var pricingService = new PricingService(store, customerRepository, couponRepository, new[] { 7 });
        // Create OrderPricer
        var orderPricer = new OrderPricer(pricingService);


        //Get Order #1 from your existing orders data
        var order = new Order
        {
            Id = 1,
            CustomerId = 1,
            Items = [new OrderItem { ProductId = 1, Quantity = 1 }
            , new OrderItem { ProductId = 3, Quantity = 1 }]
            ,
            CouponCode = "SAVE10"
            ,
            Status = OrderStatus.Validated
        };

        // Measure Parallel execution
        var stopwatch = Stopwatch.StartNew();

        await orderPricer.PriceOrderAsync(order, CancellationToken.None);

        stopwatch.Stop();
        Console.WriteLine($"Parallel: {stopwatch.ElapsedMilliseconds} ms");

        // Measure Sequential execution
        stopwatch.Restart();

        await orderPricer.PriceOrderSequentialAsync(order, CancellationToken.None);

        stopwatch.Stop();
        Console.WriteLine($"Sequential: {stopwatch.ElapsedMilliseconds} ms");


        Console.WriteLine("------------------------------------------------");

        //JsonFileStore store = new JsonFileStore();

        RaceConditionDemo raceConditionDemo =
            new RaceConditionDemo(store);

        OrderRepository orderRepository =
            new OrderRepository(store);

        ProductRepository productRepository =
            new ProductRepository(store);

        

        OrderValidator orderValidator = new OrderValidator(
            customerRepository,
            productRepository,
            couponRepository,
            orderRepository);

        OrderProcessorV2 orderProcessor = new OrderProcessorV2(
            store,
            raceConditionDemo,
            orderPricer,
            orderRepository);

        ReportGenerator reportGenerator = new ReportGenerator(
            orderRepository,
            productRepository,
            store);

        Phase2Runner phase2Runner = new Phase2Runner(
            orderProcessor,
            store,
            raceConditionDemo,
            orderPricer,
            orderValidator,
            orderRepository,
            reportGenerator);

        await phase2Runner.RunAsync();



    }

    
}