using Microsoft.Extensions.Logging;
using System;

namespace DecoratorAndDotNetDI.EmailSender
{
    class SmtpEmailMessageSender : IEmailMessageSender, IDisposable
    {
        private readonly ILogger logger;

        public SmtpEmailMessageSender(ILogger<SmtpEmailMessageSender> logger)
        {
            this.logger = logger;
            logger.LogWarning("Constructed");
        }

        public void Dispose()
        {
            logger.LogWarning("Disposing");
        }

        public void SendMessage(EmailMessage message)
        {
            // Put your real implementation here.
            logger.LogInformation($"Sending message '{message.Body}'");
        }
    }
}
