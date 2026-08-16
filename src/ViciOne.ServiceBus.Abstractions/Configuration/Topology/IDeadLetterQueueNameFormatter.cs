namespace ViciOne.ServiceBus
{
    public interface IDeadLetterQueueNameFormatter
    {
        string FormatDeadLetterQueueName(string queueName);
    }
}
