using System;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Carries the message event data.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessageEvent<TMessage> :
    TriggerEvent,
    IEvent<TMessage>,
    IEquatable<MessageEvent<TMessage>>
    where TMessage : class
{
    /// <summary>Exposes the instance used by the containing type.</summary>
    public static readonly IEvent<TMessage> Instance = new MessageEvent<TMessage>(TypeCache<TMessage>.ShortName);

    /// <summary>Initializes a new instance.</summary>
    /// <param name="name">The name.</param>
    public MessageEvent(string name)
        : base(name)
    {
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public override void Accept(IStateMachineVisitor visitor)
    {
        visitor.Visit(this, x =>
        {
        });
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public override void Probe(ProbeContext context)
    {
        base.Probe(context);

        context.Add("dataType", TypeCache<TMessage>.ShortName);
    }

    /// <summary>Determines whether this instance equals the supplied value.</summary>
    /// <param name="other">The other.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(MessageEvent<TMessage>? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Equals(other.Name, Name);
    }

    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
    public override string ToString()
    {
        return $"{Name}<{typeof(TMessage).Name}> (Event)";
    }

    /// <summary>Determines whether this instance equals the supplied value.</summary>
    /// <param name="obj">The obj.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        return Equals(obj as MessageEvent<TMessage>);
    }

    /// <summary>Gets hash code.</summary>
    /// <returns>The hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return base.GetHashCode() * 27 + typeof(TMessage).GetHashCode();
    }
}
