using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared
{
    //عملنا الكلاس ده عشان لو حصل ايه تاخير في السيستم نقدر نعمله سيموليشن ونشوف تأثيره علي السيستم
    public static class SimulatedLatency
    {
        public static async Task StepAsync(string component ,string message, int milliseconds,CancellationToken ct)
        {
            Log.Info(component, message);
            await Task.Delay(milliseconds,ct);
        }
        public static async Task StepAsync(string component, string message, int minmilliseconds, int maxmilliseconds, CancellationToken ct)
        {
            Log.Info(component, message);
            var delay = Random.Shared.Next(minmilliseconds, maxmilliseconds+1);
            await Task.Delay(delay, ct);
        }


    }
}
