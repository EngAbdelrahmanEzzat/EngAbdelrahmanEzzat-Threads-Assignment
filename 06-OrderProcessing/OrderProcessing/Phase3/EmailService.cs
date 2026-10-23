using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared;

namespace OrderProcessing.Phase3
{
    public class EmailService
    {
        private readonly JsonFileStore _store;
        private readonly Dictionary<int, int> _failAttempts;
        private readonly bool _enableFailures;
        private readonly ConcurrentDictionary<int, int> _attemptCounts = new();
        private static readonly SemaphoreSlim _emailSemaphore = new(1, 1);

        public EmailService(
            JsonFileStore store,
            IDictionary<int, int>? failAttempts = null,
            bool enableFailures = true)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _enableFailures = enableFailures;
            _failAttempts = failAttempts != null ? new Dictionary<int, int>(failAttempts) : new Dictionary<int, int>();
        }

        public EmailService(
            IDictionary<int, int>? failAttempts = null,
            bool enableFailures = true)
            : this(new JsonFileStore(), failAttempts, enableFailures)
        {
        }

        // ==================== Asynchronous Methods ====================

        public async Task<EmailMessage> SendOrderConfirmationAsync(
            Order order,
            string customerEmail,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(order);

            int attempt = _attemptCounts.AddOrUpdate(order.Id, 1, (_, current) => current + 1);

            await SimulatedLatency.StepAsync(nameof(EmailService), "Connecting to SMTP server...", 100, ct);

            if (_enableFailures && _failAttempts.TryGetValue(order.Id, out int maxFailures) && attempt <= maxFailures)
            {
                throw new SmtpUnavailableException($"SMTP server unavailable (attempt #{attempt})");
            }

            await SimulatedLatency.StepAsync(nameof(EmailService), "Sending email...", 200, ct);

            var emailMessage = new EmailMessage
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                To = customerEmail,
                Subject = $"Order #{order.Id} Confirmation",
                Body = $"Thank you! Your order #{order.Id} of amount ${order.TotalAmount:F2} has been processed.",
                SentAt = DateTime.Now,
                Attempts = attempt
            };

            await _emailSemaphore.WaitAsync(ct);
            try
            {
                var emails = await _store.ReadAsync<EmailMessage>(DataFiles.Emails, ct);
                emails.Add(emailMessage);
                await _store.WriteAsync(DataFiles.Emails, emails, ct);
            }
            finally
            {
                _emailSemaphore.Release();
            }

            return emailMessage;
        }

        public async Task SendEmailAsync(EmailMessage email, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(email);

            int attempt = _attemptCounts.AddOrUpdate(email.OrderId, 1, (_, current) => current + 1);
            email.Attempts = attempt;

            await SimulatedLatency.StepAsync(nameof(EmailService), "Connecting to SMTP server...", 100, ct);

            if (_enableFailures && _failAttempts.TryGetValue(email.OrderId, out int maxFailures) && attempt <= maxFailures)
            {
                email.LastError = $"SMTP server unavailable (attempt #{attempt})";
                throw new SmtpUnavailableException(email.LastError);
            }

            await SimulatedLatency.StepAsync(nameof(EmailService), "Sending email...", 200, ct);
            email.SentAt = DateTime.Now;

            await _emailSemaphore.WaitAsync(ct);
            try
            {
                var emails = await _store.ReadAsync<EmailMessage>(DataFiles.Emails, ct);
                emails.Add(email);
                await _store.WriteAsync(DataFiles.Emails, emails, ct);
            }
            finally
            {
                _emailSemaphore.Release();
            }
        }

        // ==================== Synchronous Methods ====================

        public EmailMessage SendOrderConfirmation(Order order, string customerEmail)
        {
            ArgumentNullException.ThrowIfNull(order);

            int attempt = _attemptCounts.AddOrUpdate(order.Id, 1, (_, current) => current + 1);

            Log.Info(nameof(EmailService), "Connecting to SMTP server...");
            Thread.Sleep(100);

            if (_enableFailures && _failAttempts.TryGetValue(order.Id, out int maxFailures) && attempt <= maxFailures)
            {
                throw new SmtpUnavailableException($"SMTP server unavailable (attempt #{attempt})");
            }

            Log.Info(nameof(EmailService), "Sending email...");
            Thread.Sleep(200);

            var emailMessage = new EmailMessage
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                To = customerEmail,
                Subject = $"Order #{order.Id} Confirmation",
                Body = $"Thank you! Your order #{order.Id} of amount ${order.TotalAmount:F2} has been processed.",
                SentAt = DateTime.Now,
                Attempts = attempt
            };

            _emailSemaphore.Wait();
            try
            {
                var emails = _store.Read<EmailMessage>(DataFiles.Emails);
                emails.Add(emailMessage);
                _store.Write(DataFiles.Emails, emails);
            }
            finally
            {
                _emailSemaphore.Release();
            }

            return emailMessage;
        }

        public void SendEmail(EmailMessage email)
        {
            ArgumentNullException.ThrowIfNull(email);

            int attempt = _attemptCounts.AddOrUpdate(email.OrderId, 1, (_, current) => current + 1);
            email.Attempts = attempt;

            Log.Info(nameof(EmailService), "Connecting to SMTP server...");
            Thread.Sleep(100);

            if (_enableFailures && _failAttempts.TryGetValue(email.OrderId, out int maxFailures) && attempt <= maxFailures)
            {
                email.LastError = $"SMTP server unavailable (attempt #{attempt})";
                throw new SmtpUnavailableException(email.LastError);
            }

            Log.Info(nameof(EmailService), "Sending email...");
            Thread.Sleep(200);
            email.SentAt = DateTime.Now;

            _emailSemaphore.Wait();
            try
            {
                var emails = _store.Read<EmailMessage>(DataFiles.Emails);
                emails.Add(email);
                _store.Write(DataFiles.Emails, emails);
            }
            finally
            {
                _emailSemaphore.Release();
            }
        }
    }

    public class SmtpUnavailableException : Exception
    {
        public SmtpUnavailableException(string message) : base(message)
        {
        }

        public SmtpUnavailableException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
