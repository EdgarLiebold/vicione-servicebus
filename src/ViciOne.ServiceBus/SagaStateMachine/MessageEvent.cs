using System;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a message event implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageEvent<TMessage> :
    TriggerEvent,
    Event<TMessage>,
    IEquatable<MessageEvent<TMessage>>
    where TMessage : class
{
    /// <summary>
    /// Defines the instance value.
    /// </summary>
    public static readonly Event<TMessage> Instance = new MessageEvent<TMessage>(TypeCache<TMessage>.ShortName);

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="name">The name value.</param>
    public MessageEvent(string name)
        : base(name)
    {
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public override void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this, x =>
        {
        });
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public override void Probe(ProbeContext context)
    {
        base.Probe(context);

        context.Add("dataType", TypeCache<TMessage>.ShortName);
    }

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(MessageEvent<TMessage>? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Equals(other.Name, Name);
    }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return $"{Name}<{typeof(TMessage).Name}> (Event)";
    }

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="obj">The obj value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        return Equals(obj as MessageEvent<TMessage>);
    }

    /// <summary>
    /// Gets hash code.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override int GetHashCode()
    {
        return base.GetHashCode() * 27 + typeof(TMessage).GetHashCode();
    }
}
