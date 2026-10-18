using OrderProcessing.Phase1;
using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared;

public class Program
{
    public static async Task Main()
    {
        using var cts = new CancellationTokenSource();

        await RunPhase1Async(cts.Token);
    }

    private static async Task RunPhase1Async(CancellationToken ct)
    {
        var store = new JsonFileStore();

        // Async repositories
        var customerRepository = new CustomerRepository(store);
        var productRepository = new ProductRepository(store);
        var couponRepository = new CouponRepository(store);
        var incomingOrderRepository = new IncomingOrderRepository(store);
        var orderRepository = new OrderRepository(store);

        // Sync repositories
        var syncCustomerRepository = new SyncCustomerRepository(store);
        var syncProductRepository = new SyncProductRepository(store);
        var syncCouponRepository = new SyncCouponRepository(store);

        // Validators
        var orderValidator = new OrderValidator(
            customerRepository,
            productRepository,
            couponRepository,
            orderRepository);

        var syncOrderValidator = new SyncOrderValidator(
            syncCustomerRepository,
            syncProductRepository,
            syncCouponRepository);

        // Phase 1 Runner
        var phase1Runner = new Phase1Runner(
            store,
            incomingOrderRepository,
            orderRepository,
            syncOrderValidator,
            orderValidator);

        await phase1Runner.RunAsync(ct);
    }
}