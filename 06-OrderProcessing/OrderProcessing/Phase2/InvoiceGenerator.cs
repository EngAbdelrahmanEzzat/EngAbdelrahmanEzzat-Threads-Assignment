
using OrderProcessing.Shared;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OrderProcessing.Phase2
{
    public static class InvoiceGenerator
    {
        public static async Task GenerateAsync(
            Order order,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(order);

            // CPU-bound work: simulate rendering the invoice.
            await Task.Run(() =>
            {
                long result = 0;

                for (int i = 0; i < 20_000_000; i++)
                {
                    result += i;
                }


            }, ct);

            // Prepare the invoice output.
            string directory = Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                "invoices");
            //المفروض ان اصلا احنا عاملين الكلام ده في الاول لاكن احنا بنأكد
            Directory.CreateDirectory(directory);

            string filePath = Path.Combine(
                directory,
                $"invoice_{order.Id}.txt");

            string content = new StringBuilder()
                .AppendLine($"Invoice for Order #{order.Id}")
                .AppendLine($"Customer ID: {order.CustomerId}")
                .AppendLine($"Status: {order.Status}")
                .AppendLine($"Subtotal: {order.SubTotal:F2}")
                .AppendLine($"Discount: {order.Discount:F2}")
                .AppendLine($"Shipping Cost: {order.ShippingCost:F2}")
                .AppendLine($"Total Amount: {order.TotalAmount:F2}")
                .ToString();

            await File.WriteAllTextAsync(filePath, content, ct);
        }
    }

}