using Microsoft.Extensions.Logging;
using System;

namespace DecoratorAndDotNetDI.EmailSender
{
    class SecondEmailSenderDecorator : IEmailMessageSender, IDisposable
    {
        private readonly ILogger logger;
        private readonly IEmailMessageSender innerSender;

        public SecondEmailSenderDecorator(
            ILogger<SecondEmailSenderDecorator> logger,
            IEmailMessageSender innerSender)
        {
            this.logger = logger;
            this.innerSender = innerSender;
            logger.LogWarning("Constructed");
        }

        public void Dispose()
        {
            logger.LogWarning("Disposing");
        }

        public void SendMessage(EmailMessage message)
        {
            logger.LogInformation("Calling inner sender...");
            innerSender.SendMessage(message);
            logger.LogInformation("Returned from inner sender.");
        }
    }
}
