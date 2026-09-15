using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides optional send topology for one message contract.</summary>
/// <typeparam name="TMessage">The sent message contract.</typeparam>
public interface IMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention
    where TMessage : class
{
    /// <summary>Attempts to resolve topology applicable to the message contract.</summary>
    /// <param name="messageSendTopology">The applicable topology when available.</param>
    /// <returns><see langword="true" /> when topology is applicable; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageSendTopology([NotNullWhen(true)] out IMessageSendTopology<TMessage>? messageSendTopology);
}


/// <summary>Projects an untyped send convention to compatible message contracts.</summary>
public interface IMessageSendTopologyConvention
{
    /// <summary>Attempts to project this convention to a message contract.</summary>
    /// <typeparam name="T">The sent message contract.</typeparam>
    /// <param name="convention">The typed convention when compatible.</param>
    /// <returns><see langword="true" /> when the convention supports <typeparamref name="T" />; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
        where T : class;
}
