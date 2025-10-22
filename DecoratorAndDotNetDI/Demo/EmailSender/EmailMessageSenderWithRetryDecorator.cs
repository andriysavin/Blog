using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace DecoratorAndDotNetDI.EmailSender
{
    class EmailMessageSenderWithRetryDecorator : IEmailMessageSender, IDisposable
    {
        private readonly IEmailMessageSender innerSender;
        private readonly ILogger logger;

        // Hardcoded for simplicity.
        private const int RetryCount = 3;
        private const int DelayBetweenRetriesMs = 1000;

        public EmailMessageSenderWithRetryDecorator(
            ILogger<EmailMessageSenderWithRetryDecorator> logger,
            IEmailMessageSender innerSender
            /* Additionally pass IOptions etc. */)
        {
            this.innerSender = innerSender;
            this.logger = logger;
            logger.LogWarning("Constructed");
        }

        public void SendMessage(EmailMessage message)
        {
            for (int i = 1; i <= RetryCount; i++)
            {
                logger.LogInformation($"Sending message, retry {i} of {RetryCount}...");

                try
                {
                    innerSender.SendMessage(message);

                    logger.LogInformation($"Message sent!");

                    return;
                }
                catch (Exception) // Catch concrete exception type in real code
                {
                    if (i == RetryCount)
                    {
                        throw;
                    }
                }

                Thread.Sleep(DelayBetweenRetriesMs);
            }
        }

        public void Dispose()
        {
            logger.LogWarning("Disposing");
        }
    }
}
