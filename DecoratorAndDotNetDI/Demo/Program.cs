using DecoratorAndDotNetDI.EmailSender;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace DecoratorAndDotNetDI
{
    class Program
    {
        static void Main()
        {
            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            // Configure 2 nested decorators.
            services.AddDecorator<IEmailMessageSender, EmailMessageSenderWithRetryDecorator>(
              decorateeServices =>
              {
                  decorateeServices.AddDecorator<IEmailMessageSender, SecondEmailSenderDecorator>(
                      decorateeServices2 =>
                          decorateeServices2.AddScoped<IEmailMessageSender, SmtpEmailMessageSender>());
              });

            using (var serviceProvider = services.BuildServiceProvider())
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("====================");

                using (var scope1 = serviceProvider.CreateScope())
                {
                    logger.LogInformation("Begin of scope1");
                    
                    logger.LogInformation("Sending message 1...");
                    var emailSender = scope1.ServiceProvider.GetRequiredService<IEmailMessageSender>();
                    emailSender.SendMessage(new EmailMessage { Body = "Msg1: Hello, decorator!" });

                    logger.LogInformation("Sending message 2...");
                    emailSender = scope1.ServiceProvider.GetRequiredService<IEmailMessageSender>();
                    emailSender.SendMessage(new EmailMessage { Body = "Msg2: Hello, decorator!" });

                    logger.LogInformation("End of scope1");
                }

                logger.LogInformation("====================");

                using (var scope2 = serviceProvider.CreateScope())
                {
                    logger.LogInformation("Begin of scope2");

                    logger.LogInformation("Sending message 3...");
                    var emailSender = scope2.ServiceProvider.GetRequiredService<IEmailMessageSender>();
                    emailSender.SendMessage(new EmailMessage { Body = "Msg3: Hello, decorator!" });

                    logger.LogInformation("End of scope2");
                }

                logger.LogInformation("====================");
            }

            // Prevent process from exiting before outputting all log events.
            Console.Read();
        }
    }
}
