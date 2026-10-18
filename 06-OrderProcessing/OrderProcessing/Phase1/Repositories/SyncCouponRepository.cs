using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase1.Repositories
{
    public class SyncCouponRepository
    {
        private readonly JsonFileStore _store;

        public SyncCouponRepository(JsonFileStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public Coupon? GetByCode(string code)
        {
            Log.Info(nameof(SyncCouponRepository), "Opening connection...");
            Thread.Sleep(200);

            int delay = Random.Shared.Next(300, 601);
            Log.Info(nameof(SyncCouponRepository), "Executing query...");
            Thread.Sleep(delay);

            List<Coupon> coupons = _store.Read<Coupon>(
                DataFiles.Coupons);

            return coupons.FirstOrDefault(
                c => string.Equals(
                    c.Code,
                    code,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}