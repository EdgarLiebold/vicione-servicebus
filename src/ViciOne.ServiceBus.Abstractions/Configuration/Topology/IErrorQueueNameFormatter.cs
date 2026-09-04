namespace ViciOne.ServiceBus;

public interface IErrorQueueNameFormatter
{
    string FormatErrorQueueName(string queueName);
}
