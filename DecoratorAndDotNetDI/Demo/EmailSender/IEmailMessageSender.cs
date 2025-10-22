namespace DecoratorAndDotNetDI.EmailSender
{
    interface IEmailMessageSender
    {
        void SendMessage(EmailMessage message);
    }
}
