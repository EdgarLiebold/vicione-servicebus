namespace ViciOne.ServiceBus.Configuration
{
    public interface IRedeliveryPipeSpecification
    {
        RedeliveryOptions Options { get; set; }
    }
}
