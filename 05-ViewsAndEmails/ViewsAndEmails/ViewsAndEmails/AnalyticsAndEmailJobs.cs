using System.Collections.Concurrent;

namespace ViewsAndEmails;

public sealed class AnalyticsAndEmailJobs
{
    public readonly ConcurrentDictionary<string, int> Views = new();
    public readonly ConcurrentQueue<string> EmailJobs = new();
    public readonly List<string> SentEmails = new();

    readonly object _sentSync = new();
    public volatile bool ProducersDone;

    public void RecordView(string product)
    {
        Views.AddOrUpdate(product, 1, (_, old) => old + 1);
    }

    public void PlaceOrder(int orderId)
    {
        EmailJobs.Enqueue($"Email: order {orderId}");
    }

    public  void EmailWorker()
    {
        while (true)
        {
           
                string? job = null;
                // TryDequeue returns false if the queue is empty, and sets job to null
                EmailJobs.TryDequeue(out job);
                if (job is not null)
                {
                    lock (_sentSync)
                        SentEmails.Add(job);
                    continue;
                }

                if (ProducersDone && EmailJobs.Count == 0)
                    return;

               Thread.Sleep(1);

            //catch (InvalidOperationException)
            //{
            //    Console.WriteLine("InvalidOperationException");
            //    if (ProducersDone)
            //        return;
            //} كود ملهوش لازمه لان ال TryDequeue مش بترمي اكسيشن لو معرقتش تخزف حاجه .
        }
    }
}
