using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using OrderProcessing;
using OrderProcessing.Shared;
namespace OrderProcessing.Phase1.Repositories
{
    public class ProductRepository
    {
        private readonly JsonFileStore  _store;
        public ProductRepository(JsonFileStore store)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            _store = store;
        }
        public async Task<Product?> GetByIdAsync(int id, CancellationToken ct)
        {
            await SimulatedLatency.StepAsync(nameof(ProductRepository), "Opining Connection...", 200, ct);
            await SimulatedLatency.StepAsync(nameof(ProductRepository), "Executing Query...", 300, 600, ct);
            var products = await _store.ReadAsync<Product>(DataFiles.Products, ct);
            return products.FirstOrDefault(p => p.Id == id);
        }

        public async Task<bool> ReserveStockAsync(int productId, int qty,CancellationToken ct)
        {
            await SimulatedLatency.StepAsync(nameof(ProductRepository), "Opining Connection...", 200, ct);

            await SimulatedLatency.StepAsync(nameof(ProductRepository), "Beginning transaction...", 100, ct);
            List<Product> products = await _store.ReadAsync<Product>(DataFiles.Products, ct);

            foreach (var product in products)
            {
                if(product.Id==productId&&product.StockQuantity>=qty)
                {
                    await SimulatedLatency.StepAsync(nameof(ProductRepository), "Updating product stock...", 400, ct);
                    product.StockQuantity -= qty;
                    await _store.WriteAsync<Product>(DataFiles.Products, products, ct);
                    await SimulatedLatency.StepAsync(nameof(ProductRepository), "Committing transaction...",  200, ct);
                    return true;
                }
            }
            return false;
          
            

        }
    
    }
}
