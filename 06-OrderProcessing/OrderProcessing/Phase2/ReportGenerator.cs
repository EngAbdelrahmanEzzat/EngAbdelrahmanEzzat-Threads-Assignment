using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
 
namespace OrderProcessing.Phase2
    { 
public class ReportGenerator
{
    private readonly OrderRepository _orderRepository;
    private readonly ProductRepository _productRepository;
    private readonly JsonFileStore _store;

    public ReportGenerator(
        OrderRepository orderRepository,
        ProductRepository productRepository,
        JsonFileStore store)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _store = store;
    }

    public async Task<Report> GenerateAsync(
        CancellationToken ct = default)
    {
        // Load orders and products asynchronously.
        List<Order> orders =
            await _orderRepository.GetAllAsync(ct);

            List<Product> products = await _store.ReadAsync<Product>(DataFiles.Products, ct);
        // Count orders by status.
        int completedCount = orders.Count(
            o => o.Status == OrderStatus.Completed);

        int rejectedCount = orders.Count(
            o => o.Status == OrderStatus.Rejected);

        int failedCount = orders.Count(
            o => o.Status == OrderStatus.Failed);

        int paymentFailedCount = orders.Count(
            o => o.Status == OrderStatus.PaymentFailed);

        // Revenue includes Completed orders only.
        decimal totalRevenue = orders
            .Where(o => o.Status == OrderStatus.Completed)
            .Sum(o => o.TotalAmount);

        decimal averageOrderValue = completedCount == 0
            ? 0
            : totalRevenue / completedCount;

        // Calculate total quantity sold for each product
        // across Completed orders only.
        var topSelling = orders
            .Where(o => o.Status == OrderStatus.Completed)
            .SelectMany(o => o.Items)
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .OrderByDescending(item => item.Quantity)
            .ThenBy(item => item.ProductId)
            .FirstOrDefault();

        string topSellingProduct = topSelling is null
            ? "None"
            : products.FirstOrDefault(
                p => p.Id == topSelling.ProductId)?.Name ?? "Unknown";

        int topSellingQuantity = topSelling?.Quantity ?? 0;

        Report report = new Report
        {
            TotalOrders = orders.Count,

            CompletedCount = completedCount,
            RejectedCount = rejectedCount,
            FailedCount = failedCount,
            PaymentFailedCount = paymentFailedCount,

            TotalRevenue = Math.Round(
                totalRevenue, 2, MidpointRounding.AwayFromZero),

            AverageOrderValue = Math.Round(
                averageOrderValue, 2, MidpointRounding.AwayFromZero),

            TopSellingProduct = topSellingProduct,
            TopSellingQuantity = topSellingQuantity
        };

        // Save report to Data/reports/report_{timestamp}.json
        string directory = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "reports");

        Directory.CreateDirectory(directory);

        string fileName =
            $"report_{DateTime.Now:yyyyMMdd_HHmmss}.json";

        string filePath = Path.Combine(directory, fileName);

        string json = System.Text.Json.JsonSerializer.Serialize(
            report,
            JsonConfig.Options);

        await File.WriteAllTextAsync(filePath, json, ct);

        return report;
    }
}

}