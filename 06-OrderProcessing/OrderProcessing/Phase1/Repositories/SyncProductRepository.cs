using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase1.Repositories
{
    public class SyncProductRepository
    {
        private readonly JsonFileStore _store;

        public SyncProductRepository(JsonFileStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public Product? GetById(int id)
        {
            Log.Info(nameof(SyncProductRepository), "Opening connection...");
            Thread.Sleep(200);

            int delay = Random.Shared.Next(300, 601);
            Log.Info(nameof(SyncProductRepository), "Executing query...");
            Thread.Sleep(delay);

            List<Product> products = _store.Read<Product>(
                DataFiles.Products);

            return products.FirstOrDefault(p => p.Id == id);
        }

        public bool ReserveStock(int productId, int qty)
        {
            Log.Info(nameof(SyncProductRepository), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(SyncProductRepository), "Beginning transaction...");
            Thread.Sleep(100);

            List<Product> products = _store.Read<Product>(
                DataFiles.Products);

            foreach (var product in products)
            {
                if (product.Id == productId && product.StockQuantity >= qty)
                {
                    Log.Info(nameof(SyncProductRepository), "Updating product stock...");
                    Thread.Sleep(400);
                    product.StockQuantity -= qty;
                    _store.Write(DataFiles.Products, products);

                    Log.Info(nameof(SyncProductRepository), "Committing transaction...");
                    Thread.Sleep(200);
                    return true;
                }
            }
            return false;
           

          
        }
    }
}