using System;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Applies message-specific publish topology to a publish pipeline.</summary>
/// <typeparam name="TMessage">The published message contract.</typeparam>
public interface IMessagePublishTopology<TMessage> :
    IMessagePublishTopology
    where TMessage : class
{
    /// <summary>Applies message-specific publish topology to a publish-pipe builder.</summary>
    /// <param name="builder">The publish-pipe topology builder.</param>
    void Apply(ITopologyPipeBuilder<PublishContext<TMessage>> builder);
}


/// <summary>Describes broker publication for one message contract.</summary>
public interface IMessagePublishTopology
{
    /// <summary>Gets whether broker topology creation excludes this message contract.</summary>
    bool Exclude { get; }

    /// <summary>
    /// Resolves the publish address before a transport-specific publish context exists.
    /// </summary>
    /// <param name="baseAddress">The host base address used to construct the broker entity address.</param>
    /// <param name="publishAddress">The resolved publish address when available.</param>
    /// <returns><see langword="true" /> when an address is available; otherwise, <see langword="false" />.</returns>
    bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress);
}
