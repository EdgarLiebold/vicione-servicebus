using System;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Identifies a named state-machine event carrying a message contract.</summary>
/// <typeparam name="TMessage">The message contract carried by the event.</typeparam>
public class MessageEvent<TMessage> :
    TriggerEvent,
    IEvent<TMessage>,
    IEquatable<MessageEvent<TMessage>>
    where TMessage : class
{
    /// <summary>Exposes the singleton event named from the cached message-contract display name.</summary>
    public static readonly IEvent<TMessage> Instance = new MessageEvent<TMessage>(TypeCache<TMessage>.ShortName);

    /// <summary>Initializes a message event with the supplied name.</summary>
    /// <param name="name">The non-null event name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is null.</exception>
    public MessageEvent(string name)
        : base(name)
    {
    }

    /// <summary>Visits this event through the message-specific event overload.</summary>
    /// <param name="visitor">The visitor receiving this event and its message contract.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor" /> is null.</exception>
    public override void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);

        visitor.Visit(this, x =>
        {
        });
    }

    /// <summary>Writes the event name and cached message-contract display name to the diagnostic context.</summary>
    /// <param name="context">The context receiving the event metadata.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is null.</exception>
    public override void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        base.Probe(context);

        context.Add("dataType", TypeCache<TMessage>.ShortName);
    }

    /// <summary>Compares another event from the same closed message-event hierarchy by ordinal name.</summary>
    /// <param name="other">The message event compared with this instance.</param>
    /// <returns><see langword="true" /> when <paramref name="other" /> has the same ordinal name; otherwise, <see langword="false" />.</returns>
    public bool Equals(MessageEvent<TMessage>? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return string.Equals(other.Name, Name, StringComparison.Ordinal);
    }

    /// <summary>Returns the event name, CLR message-type name and event marker.</summary>
    /// <returns>The display text in the form <c>Name&lt;MessageType&gt; (Event)</c>.</returns>
    public override string ToString()
    {
        return $"{Name}<{typeof(TMessage).Name}> (Event)";
    }

    /// <summary>Compares an object from the same closed message-event hierarchy by ordinal name.</summary>
    /// <param name="obj">The object compared with this instance.</param>
    /// <returns><see langword="true" /> when <paramref name="obj" /> is a compatible message event with the same ordinal name; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        return Equals(obj as MessageEvent<TMessage>);
    }

    /// <summary>Combines the name hash with the carried message type.</summary>
    /// <returns>The hash code for this closed message-event type and name.</returns>
    public override int GetHashCode()
    {
        return base.GetHashCode() * 27 + typeof(TMessage).GetHashCode();
    }
}
