using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Phase3;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase4
{
    public static class Task10_4_FraudAndEmail
    {
        // ==========================================================
        // Part A: Fraud scoring (CPU-bound)
        // ==========================================================

        public static int Score(int orderId)
        {
            long h = orderId;
            for (int i = 0; i < 100_000; i++)
            {
                h = (h * 31 + i) % 1_000_000_007;
            }
            return (int)(h % 100);
        }

        public static void RunFraudScoring(CancellationToken ct = default)
        {
            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine("          Part A: CPU-bound Fraud Scoring (10,000 Orders)       ");
            Console.WriteLine("----------------------------------------------------------------\n");

            const int totalOrders = 10_000;
            const int scoreThreshold = 90;

            // R1: Plain sequential loop
            Console.WriteLine("--> Running Sequential Fraud Scoring...");
            var swSeq = Stopwatch.StartNew();
            var seqFlagged = new List<int>();

            for (int id = 1; id <= totalOrders; id++)
            {
                ct.ThrowIfCancellationRequested();
                if (Score(id) >= scoreThreshold)
                {
                    seqFlagged.Add(id);
                }
            }
            swSeq.Stop();

            // R2: Parallel run using Parallel.For spread across all cores
            Console.WriteLine("--> Running Parallel Fraud Scoring (MaxDegreeOfParallelism = ProcessorCount)...");
            var distinctThreads = new ConcurrentDictionary<int, byte>();
            var parallelFlagged = new ConcurrentBag<int>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct
            };

            var swPar = Stopwatch.StartNew();
            Parallel.For(1, totalOrders + 1, parallelOptions, id =>
            {
                distinctThreads.TryAdd(Environment.CurrentManagedThreadId, 0);
                if (Score(id) >= scoreThreshold)
                {
                    parallelFlagged.Add(id);
                }
            });
            swPar.Stop();

            var sortedParallelFlagged = parallelFlagged.OrderBy(id => id).ToList();

            // R3: Both return exact same result (1005 flagged orders, first five: 4, 8, 12, 29, 33)
            bool listsMatch = seqFlagged.SequenceEqual(sortedParallelFlagged);
            double speedup = (double)swSeq.ElapsedMilliseconds / Math.Max(1, swPar.ElapsedMilliseconds);

            // R5: Print results
            Console.WriteLine($"\nProcessor Count:              {Environment.ProcessorCount}");
            Console.WriteLine($"Distinct Threads Used:        {distinctThreads.Count}");
            Console.WriteLine($"Sequential Time:              {swSeq.ElapsedMilliseconds} ms");
            Console.WriteLine($"Parallel Time:                {swPar.ElapsedMilliseconds} ms");
            Console.WriteLine($"Speed-up Factor:              {speedup:F2}x");
            Console.WriteLine($"Flagged Count (Sequential):   {seqFlagged.Count}");
            Console.WriteLine($"Flagged Count (Parallel):     {sortedParallelFlagged.Count}");
            Console.WriteLine($"First 5 Flagged IDs:          {string.Join(", ", sortedParallelFlagged.Take(5))}");
            Console.WriteLine($"Results Match Exact:          {listsMatch && sortedParallelFlagged.Count == 1005}\n");
        }

        // ==========================================================
        // Part B: Email re-send (I/O-bound)
        // ==========================================================

        public static async Task RunEmailResendAsync(CancellationToken ct = default)
        {
            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine("         Part B: I/O-bound Email Re-send (Parallel.ForEachAsync) ");
            Console.WriteLine("----------------------------------------------------------------\n");

            var store = new JsonFileStore();
            var emailService = new EmailService(store, enableFailures: false);

            // Create 30 test emails (order ids 1001–1030)
            var emails = Enumerable.Range(1001, 30).Select(id => new EmailMessage
            {
                Id = Guid.NewGuid(),
                OrderId = id,
                To = $"customer_{id}@example.com",
                Subject = $"Order #{id} Re-send",
                Body = $"Your order #{id} notification."
            }).ToList();

            int currentConcurrent = 0;
            int maxConcurrentObserved = 0;

            // R1: Concurrency limit = 4 from the library (Parallel.ForEachAsync)
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,
                CancellationToken = ct
            };

            Console.WriteLine("--> Sending 30 emails with MaxDegreeOfParallelism = 4...\n");
            var sw = Stopwatch.StartNew();

            await Parallel.ForEachAsync(emails, parallelOptions, async (email, token) =>
            {
                // R2: Track concurrency accurately
                int current = Interlocked.Increment(ref currentConcurrent);

                int initial, computedMax;
                do
                {
                    initial = maxConcurrentObserved;
                    computedMax = Math.Max(initial, current);
                }
                while (Interlocked.CompareExchange(ref maxConcurrentObserved, computedMax, initial) != initial);

                try
                {
                    await emailService.SendEmailAsync(email, token);
                }
                finally
                {
                    Interlocked.Decrement(ref currentConcurrent);
                }
            });

            sw.Stop();

            // R3: All 30 emails end up in emails.json
            var savedEmails = await store.ReadAsync<EmailMessage>(DataFiles.Emails, ct);
            var reSentEmails = savedEmails.Where(e => e.OrderId >= 1001 && e.OrderId <= 1030).ToList();

            // R4: Print total time and results
            Console.WriteLine($"Total Elapsed Time:           {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F2} s)");
            Console.WriteLine($"Target Concurrency Limit:     4");
            Console.WriteLine($"Peak Concurrent Sends Seen:   {maxConcurrentObserved}");
            Console.WriteLine($"Emails Sent and Verified:     {reSentEmails.Count} / 30");
            Console.WriteLine($"Concurrency Requirement Met:  {maxConcurrentObserved == 4 && reSentEmails.Count == 30}\n");
        }
    }
}
