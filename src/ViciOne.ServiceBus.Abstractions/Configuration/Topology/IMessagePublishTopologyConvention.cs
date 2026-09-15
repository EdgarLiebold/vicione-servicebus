using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides optional publish topology for one message contract.</summary>
/// <typeparam name="TMessage">The published message contract.</typeparam>
public interface IMessagePublishTopologyConvention<TMessage> :
    IMessagePublishTopologyConvention
    where TMessage : class
{
    /// <summary>Attempts to resolve topology applicable to the message contract.</summary>
    /// <param name="messagePublishTopology">The applicable topology when available.</param>
    /// <returns><see langword="true" /> when topology is applicable; otherwise, <see langword="false" />.</returns>
    bool TryGetMessagePublishTopology([NotNullWhen(true)] out IMessagePublishTopology<TMessage>? messagePublishTopology);
}


/// <summary>Projects an untyped publish convention to compatible message contracts.</summary>
public interface IMessagePublishTopologyConvention
{
    /// <summary>Attempts to project this convention to a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="convention">The typed convention when compatible.</param>
    /// <returns><see langword="true" /> when the convention supports <typeparamref name="T" />; otherwise, <see langword="false" />.</returns>
    bool TryGetMessagePublishTopologyConvention<T>([NotNullWhen(true)] out IMessagePublishTopologyConvention<T>? convention)
        where T : class;
}
