// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Configuration;


    /// <summary>
    /// Specification for configuring a receive endpoint
    /// </summary>
    public interface IReceiveEndpointSpecification :
        ISpecification
    {
        void Configure(IReceiveEndpointBuilder builder);
    }
}
