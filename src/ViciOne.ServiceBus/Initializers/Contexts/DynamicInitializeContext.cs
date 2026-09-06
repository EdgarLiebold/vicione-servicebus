using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Initializers.Contexts;

/// <summary>Carries state for dynamic initialize operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class DynamicInitializeContext<TMessage> :
    ProxyPipeContext,
    InitializeContext<TMessage>
    where TMessage : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="message">The message to process.</param>
    public DynamicInitializeContext(InitializeContext context, TMessage message)
        : base(context)
    {
        Message = message;
        MessageType = message.GetType();

        Depth = context.Depth + 1;
        Parent = context;
    }

    /// <summary>Gets the message.</summary>
    public TMessage Message { get; }
    /// <summary>Gets the message type.</summary>
    public Type MessageType { get; }

    /// <summary>Gets the depth.</summary>
    public int Depth { get; }

    /// <summary>Gets the parent.</summary>
    public InitializeContext Parent { get; }

    /// <summary>Creates input context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="input">The input.</param>
    /// <returns>The created input context.</returns>
    public InitializeContext<TMessage, T> CreateInputContext<T>(T input)
        where T : class
    {
        return new DynamicInitializeContext<TMessage, T>(this, Message, input);
    }

    /// <summary>Attempts to get parent.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="parentContext">Receives the parent context produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetParent<T>([NotNullWhen(true)] out InitializeContext<T>? parentContext)
        where T : class
    {
        InitializeContext<T>? parent;
        if (this is InitializeContext<T> current)
            parent = current;
        else if (!Parent.TryGetParent(out parent))
            parent = null;

        if (parent != null)
        {
            parentContext = parent;
            return true;
        }

        parentContext = default;
        return false;
    }

    /// <summary>Creates message context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <returns>The created message context.</returns>
    public InitializeContext<T> CreateMessageContext<T>(T message)
        where T : class
    {
        return new DynamicInitializeContext<T>(this, message);
    }
}


/// <summary>Carries state for dynamic initialize operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class DynamicInitializeContext<TMessage, TInput> :
    DynamicInitializeContext<TMessage>,
    InitializeContext<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="input">The input.</param>
    public DynamicInitializeContext(InitializeContext context, TMessage message, TInput input)
        : base(context, message)
    {
        HasInput = (Input = input) != null;
    }

    /// <summary>Gets a value indicating whether this instance has input.</summary>
    public bool HasInput { get; }
    /// <summary>Gets the input.</summary>
    public TInput Input { get; } = null!;
}
