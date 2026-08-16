namespace ViciOne.ServiceBus
{
    public interface IDefinition
    {
        int? ConcurrentMessageLimit { get; }
    }
}
