using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Initializers.Contexts;

/// <summary>Represents one typed message node in a nested initialization graph.</summary>
/// <typeparam name="TMessage">The message contract owned by the node.</typeparam>
internal class DynamicInitializeContext<TMessage> :
    ProxyPipeContext,
    InitializeContext<TMessage>
    where TMessage : class
{
    /// <summary>Creates a child node for a message instance.</summary>
    /// <param name="context">The parent initialization context.</param>
    /// <param name="message">The message instance owned by this node.</param>
    public DynamicInitializeContext(InitializeContext context, TMessage message)
        : this(context, message, context?.Depth + 1 ?? 0, context!)
    {
    }

    /// <summary>Creates another typed view of an existing message node.</summary>
    /// <param name="context">The pipeline context inherited by the view.</param>
    /// <param name="message">The message instance owned by the node.</param>
    /// <param name="depth">The node's depth in the initialized object graph.</param>
    /// <param name="parent">The node's containing initialization context.</param>
    protected DynamicInitializeContext(InitializeContext context, TMessage message, int depth, InitializeContext parent)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        MessageType = message.GetType();

        Depth = depth;
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    /// <summary>Gets the message owned by this node.</summary>
    public TMessage Message { get; }
    /// <summary>Gets the runtime type of <see cref="Message"/>.</summary>
    public Type MessageType { get; }

    /// <summary>Gets this node's nesting depth.</summary>
    public int Depth { get; }

    /// <summary>Gets the parent initialization node.</summary>
    public InitializeContext Parent { get; }

    /// <summary>Associates an input object with this message node.</summary>
    /// <typeparam name="T">The input-object type.</typeparam>
    /// <param name="input">The input object used to populate the message.</param>
    /// <returns>A context containing the current message and <paramref name="input"/>.</returns>
    public InitializeContext<TMessage, T> CreateInputContext<T>(T input)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(input);
        return new DynamicInitializeContext<TMessage, T>(this, Message, input);
    }

    /// <summary>Finds the nearest node whose message implements the requested contract.</summary>
    /// <typeparam name="T">The requested parent message contract.</typeparam>
    /// <param name="parentContext">Receives the matching node when one exists.</param>
    /// <returns><see langword="true"/> when the current or an ancestor node matches.</returns>
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

    /// <summary>Creates a nested message node below this node.</summary>
    /// <typeparam name="T">The nested message contract.</typeparam>
    /// <param name="message">The nested message instance.</param>
    /// <returns>A child context for <paramref name="message"/>.</returns>
    public InitializeContext<T> CreateMessageContext<T>(T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return new DynamicInitializeContext<T>(this, message);
    }
}


/// <summary>Associates a typed input object with a message node.</summary>
/// <typeparam name="TMessage">The message contract owned by the node.</typeparam>
/// <typeparam name="TInput">The input object associated with the message.</typeparam>
internal sealed class DynamicInitializeContext<TMessage, TInput> :
    DynamicInitializeContext<TMessage>,
    InitializeContext<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    /// <summary>Creates an input-bearing message context.</summary>
    /// <param name="context">The parent initialization context.</param>
    /// <param name="message">The message being populated.</param>
    /// <param name="input">The input object used to populate the message.</param>
    public DynamicInitializeContext(InitializeContext context, TMessage message, TInput input)
        : base(context, message, context?.Depth ?? 0, context?.Parent!)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        HasInput = true;
    }

    /// <summary>Gets a value indicating that this context contains an input object.</summary>
    public bool HasInput { get; }
    /// <summary>Gets the input object associated with the message.</summary>
    public TInput Input { get; }
}
