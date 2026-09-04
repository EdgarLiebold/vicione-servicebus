using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

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
