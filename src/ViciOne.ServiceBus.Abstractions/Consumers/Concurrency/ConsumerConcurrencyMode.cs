namespace ViciOne.ServiceBus;

public enum ConsumerConcurrencyMode
{
    Parallel = 0,
    Serial = 1,
    Partitioned = 2
}
