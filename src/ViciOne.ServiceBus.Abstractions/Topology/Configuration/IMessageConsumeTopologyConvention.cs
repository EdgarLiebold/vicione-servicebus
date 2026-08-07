// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using System.Diagnostics.CodeAnalysis;


    public interface IMessageConsumeTopologyConvention<TMessage> :
        IMessageConsumeTopologyConvention
        where TMessage : class
    {
        bool TryGetMessageConsumeTopology([NotNullWhen(true)] out IMessageConsumeTopology<TMessage>? messageConsumeTopology);
    }


    public interface IMessageConsumeTopologyConvention
    {
        bool TryGetMessageConsumeTopologyConvention<T>([NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
            where T : class;
    }
}
