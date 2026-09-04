using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Provides a message topic exchange implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class MessageTopicExchange<T> :
    IMessageExchange<T>
    where T : class
{
    readonly TopicNode<T> _root;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="comparer">The comparer value.</param>
    public MessageTopicExchange(string name, StringComparer? comparer = default)
    {
        Name = name;

        _root = new TopicNode<T>(comparer ?? StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the sinks value.
    /// </summary>
    public IEnumerable<IMessageSink<T>> Sinks => _root.Sinks;

    /// <summary>
    /// Gets the name value.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Performs the deliver operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DeliverAsync(DeliveryContext<T> context, CancellationToken cancellationToken = default)
    {
        var routingKey = context.RoutingKey;

        return _root.DeliverAsync(context, routingKey, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the connect operation.
    /// </summary>
    /// <param name="sink">The sink value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle Connect(IMessageSink<T> sink, string? routingKey)
    {
        return _root.Add(sink, routingKey);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("exchange");
        scope.Add("name", Name);
        scope.Add("type", "topic");

        var topicScope = scope.CreateScope("topics");

        _root.Probe(topicScope);
    }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return $"Exchange({Name})";
    }
}
