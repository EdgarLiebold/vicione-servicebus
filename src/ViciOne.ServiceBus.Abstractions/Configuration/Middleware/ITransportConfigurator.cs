namespace ViciOne.ServiceBus
{
    public interface ITransportConfigurator
    {
        int PrefetchCount { set; }

        int? ConcurrentMessageLimit { set; }
    }
}
