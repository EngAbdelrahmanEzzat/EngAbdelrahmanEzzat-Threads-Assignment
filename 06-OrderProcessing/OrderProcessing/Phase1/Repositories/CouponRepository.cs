using OrderProcessing;
using OrderProcessing.Shared;
using System;
using System.Collections.Generic;
using System.Text;
namespace OrderProcessing.Phase1.Repositories
{
    public class CouponRepository
    {
        private readonly JsonFileStore _store;
        public CouponRepository(JsonFileStore store)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            _store = store;
        }
        public async Task<Coupon?>GetByCodeAsync(string code,CancellationToken ct)
        {
            await SimulatedLatency.StepAsync(nameof(CouponRepository), "Opining Connection...", 200, ct);
            await SimulatedLatency.StepAsync(nameof(CouponRepository), "Executing Query...", 300, 600, ct);
            var coupons = await _store.ReadAsync<Coupon>(DataFiles.Coupons, ct);
            return coupons.FirstOrDefault(c => string.Equals(c.Code,code,StringComparison.OrdinalIgnoreCase));

        }

    }
}
