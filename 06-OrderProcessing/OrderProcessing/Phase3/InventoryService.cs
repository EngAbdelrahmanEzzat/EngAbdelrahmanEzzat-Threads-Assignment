using OrderProcessing.Phase2;
using OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderProcessing.Phase3
{
    public class InventoryService
    {
        private readonly SemaphoreSlim _stockSemaphore = new(1, 1);
        private readonly JsonFileStore _store;

        public InventoryService(JsonFileStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public InventoryService()
            : this(new JsonFileStore())
        {
        }

        // ==================== Asynchronous Methods ====================

        public async Task<bool> ReserveStockAsync(Order order, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(order);

            await _stockSemaphore.WaitAsync(ct);
            try
            {
                List<Product> products = await _store.ReadAsync<Product>(DataFiles.Products, ct);
                List<(int ProductId, int Quantity)> reservedItems = new();
                foreach (OrderItem item in order.Items)
                {
                    if (item.Quantity <= 0)
                    {
                        await RollbackAsync(products, reservedItems, ct);
                        return false;
                    }
                    Product? product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product is null || product.StockQuantity < item.Quantity)
                    {
                        await RollbackAsync(products, reservedItems, ct);
                        return false;
                    }
                    product.StockQuantity -= item.Quantity;
                    reservedItems.Add((item.ProductId, item.Quantity));
                    await SimulatedLatency.StepAsync(nameof(InventoryService), "Opening Connection......", 200, ct);
                    await SimulatedLatency.StepAsync(nameof(InventoryService), "Updating stock...", 400, ct);
                    await _store.WriteAsync(DataFiles.Products, products, ct);
                }
                return true;
            }
            finally
            {
                _stockSemaphore.Release();
            }
        }

        public async Task ReleaseStockAsync(Order order, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(order);

            await _stockSemaphore.WaitAsync(ct);
            try
            {
                List<Product> products = await _store.ReadAsync<Product>(DataFiles.Products, ct);

                foreach (OrderItem item in order.Items)
                {
                    if (item.Quantity <= 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(item.Quantity), "Quantity must be greater than zero.");
                    }
                    Product? product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product is null)
                    {
                        throw new InvalidOperationException($"Product #{item.ProductId} not found.");
                    }

                    product.StockQuantity += item.Quantity;
                }
                await SimulatedLatency.StepAsync(nameof(InventoryService), "Opening Connection...", 200, ct);
                await SimulatedLatency.StepAsync(nameof(InventoryService), "Beginning Transaction...", 100, ct);
                await SimulatedLatency.StepAsync(nameof(InventoryService), "Updating Stock...", 400, ct);
                await _store.WriteAsync(DataFiles.Products, products, ct);
                await SimulatedLatency.StepAsync(nameof(InventoryService), "Committing Transaction...", 200, ct);
            }
            finally
            {
                _stockSemaphore.Release();
            }
        }

        private async Task RollbackAsync(List<Product> products, List<(int ProductId, int Quantity)> reservedItems, CancellationToken ct)
        {
            foreach (var reservedItem in reservedItems)
            {
                Product? product = products.FirstOrDefault(p => p.Id == reservedItem.ProductId);
                if (product is not null)
                {
                    product.StockQuantity += reservedItem.Quantity;
                }
            }
            await _store.WriteAsync(DataFiles.Products, products, ct);
        }

        // ==================== Synchronous Methods ====================

        public bool ReserveStock(Order order)
        {
            ArgumentNullException.ThrowIfNull(order);

            _stockSemaphore.Wait();
            try
            {
                List<Product> products = _store.Read<Product>(DataFiles.Products);
                List<(int ProductId, int Quantity)> reservedItems = new();
                foreach (OrderItem item in order.Items)
                {
                    if (item.Quantity <= 0)
                    {
                        Rollback(products, reservedItems);
                        return false;
                    }
                    Product? product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product is null || product.StockQuantity < item.Quantity)
                    {
                        Rollback(products, reservedItems);
                        return false;
                    }
                    product.StockQuantity -= item.Quantity;
                    reservedItems.Add((item.ProductId, item.Quantity));

                    Log.Info(nameof(InventoryService), "Opening Connection......");
                    Thread.Sleep(200);

                    Log.Info(nameof(InventoryService), "Updating stock...");
                    Thread.Sleep(400);

                    _store.Write(DataFiles.Products, products);
                }
                return true;
            }
            finally
            {
                _stockSemaphore.Release();
            }
        }

        public void ReleaseStock(Order order)
        {
            ArgumentNullException.ThrowIfNull(order);

            _stockSemaphore.Wait();
            try
            {
                List<Product> products = _store.Read<Product>(DataFiles.Products);

                foreach (OrderItem item in order.Items)
                {
                    if (item.Quantity <= 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(item.Quantity), "Quantity must be greater than zero.");
                    }
                    Product? product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product is null)
                    {
                        throw new InvalidOperationException($"Product #{item.ProductId} not found.");
                    }

                    product.StockQuantity += item.Quantity;
                }

                Log.Info(nameof(InventoryService), "Opening Connection...");
                Thread.Sleep(200);

                Log.Info(nameof(InventoryService), "Beginning Transaction...");
                Thread.Sleep(100);

                Log.Info(nameof(InventoryService), "Updating Stock...");
                Thread.Sleep(400);

                _store.Write(DataFiles.Products, products);

                Log.Info(nameof(InventoryService), "Committing Transaction...");
                Thread.Sleep(200);
            }
            finally
            {
                _stockSemaphore.Release();
            }
        }

        private void Rollback(List<Product> products, List<(int ProductId, int Quantity)> reservedItems)
        {
            foreach (var reservedItem in reservedItems)
            {
                Product? product = products.FirstOrDefault(p => p.Id == reservedItem.ProductId);
                if (product is not null)
                {
                    product.StockQuantity += reservedItem.Quantity;
                }
            }
            _store.Write(DataFiles.Products, products);
        }
    }
}
