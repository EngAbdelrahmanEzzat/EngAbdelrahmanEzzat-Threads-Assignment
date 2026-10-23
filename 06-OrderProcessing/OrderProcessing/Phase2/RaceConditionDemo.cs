using Microsoft.VisualBasic;
using OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;

namespace OrderProcessing.Phase2
{
    
    public class RaceConditionDemo
    {
        private readonly SemaphoreSlim _stockSemaphore = new(1, 1);
        private readonly JsonFileStore _store;

        public RaceConditionDemo(JsonFileStore store)
        {
            _store = store;
        }
        public async Task<bool> ReserveStockAsync(int productId, int qty, CancellationToken ct)
        {

            
            if (qty <= 0) 
            {
                return false;
            }
            await _stockSemaphore.WaitAsync(ct);
            try
            {
                var products = await _store.ReadAsync<Product>(DataFiles.Products, ct);

                Product? product = products.FirstOrDefault(P => P.Id == productId);
                if (product == null)
                {
                    return false;
                }
                if (product.StockQuantity < qty)
                {
                    return false;
                }
                await SimulatedLatency.StepAsync(nameof(RaceConditionDemo), "Opening connection...", 200, ct);
                await SimulatedLatency.StepAsync(nameof(RaceConditionDemo), "Beginning transaction...", 100, ct);
                

                int newStock = product.StockQuantity - qty;
                await SimulatedLatency.StepAsync(nameof(RaceConditionDemo), "Updating stock...", 400, ct);
                product.StockQuantity = newStock;
                await _store.WriteAsync(DataFiles.Products, products, ct);
                return true;
            }
            finally
            {
                _stockSemaphore.Release();
            }
        }
    
        public async Task RunAsync(CancellationToken ct = default)
        {
            List<Product> products =
                await _store.ReadAsync<Product>(DataFiles.Products, ct);

            Product? keyboard = products.FirstOrDefault(p => p.Id == 4);

            if (keyboard is null)
            {
                throw new InvalidOperationException("Product #4 not found.");
            }

            keyboard.StockQuantity = 1;

            // Save the initial stock before testing the reservations.
            await _store.WriteAsync(DataFiles.Products, products, ct);

            Console.WriteLine(
                $"Initial keyboard stock: {keyboard.StockQuantity}");

            Task<bool> order1 = ReserveStockAsync(4, 1, ct);
            Task<bool> order2 = ReserveStockAsync(4, 1, ct);

            bool[] results;

            try
            {
                results = await Task.WhenAll(order1, order2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reservation error: {ex.Message}");
                throw;
            }

            List<Product> updatedProducts =
                await _store.ReadAsync<Product>(DataFiles.Products, ct);

            Product? updatedKeyboard =
                updatedProducts.FirstOrDefault(p => p.Id == 4);

            Console.WriteLine("\nReservation results:");

            Console.WriteLine(
                $"Order 1: {(results[0] ? "Reserved keyboard" : "Failed")}");

            Console.WriteLine(
                $"Order 2: {(results[1] ? "Reserved keyboard" : "Failed")}");

            Console.WriteLine(
                $"Keyboard stock read from file: {updatedKeyboard?.StockQuantity}");
        }


    
    public async Task ReleaseStockAsync(
        int productId,
        int qty,
        CancellationToken ct = default)
        {
            if (qty <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(qty),
                    "Quantity must be greater than zero.");
            }

            await _stockSemaphore.WaitAsync(ct);

            try
            {
                List<Product> products =
                    await _store.ReadAsync<Product>(
                        DataFiles.Products, ct);

                Product? product =
                    products.FirstOrDefault(p => p.Id == productId);

                if (product is null)
                {
                    throw new InvalidOperationException(
                        $"Product #{productId} not found.");
                }

                await SimulatedLatency.StepAsync(
                    nameof(RaceConditionDemo),
                    "Opening connection...",
                    200,
                    ct);

                await SimulatedLatency.StepAsync(
                    nameof(RaceConditionDemo),
                    "Beginning transaction...",
                    100,
                    ct);

                await SimulatedLatency.StepAsync(
                    nameof(RaceConditionDemo),
                    "Updating stock...",
                    400,
                    ct);

                product.StockQuantity += qty;

                await _store.WriteAsync(
                    DataFiles.Products, products, ct);
            }
            finally
            {
                _stockSemaphore.Release();
            }
        }



    }
}
