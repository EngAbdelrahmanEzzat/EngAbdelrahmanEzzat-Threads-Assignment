using OrderProcessing.Shared;

namespace OrderProcessing
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            
            await DataSeeder.ResetAsync(CancellationToken.None);

            Console.WriteLine("Data seeding completed successfully!");
        }
    }
}
