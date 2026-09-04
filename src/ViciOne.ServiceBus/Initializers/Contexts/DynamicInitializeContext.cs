using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Initializers.Contexts;

public class DynamicInitializeContext<TMessage> :
    ProxyPipeContext,
    InitializeContext<TMessage>
    where TMessage : class
{
    public DynamicInitializeContext(InitializeContext context, TMessage message)
        : base(context)
    {
        Message = message;
        MessageType = message.GetType();

        Depth = context.Depth + 1;
        Parent = context;
    }

    public TMessage Message { get; }
    public Type MessageType { get; }

    public int Depth { get; }

    public InitializeContext Parent { get; }

    public InitializeContext<TMessage, T> CreateInputContext<T>(T input)
        where T : class
    {
        return new DynamicInitializeContext<TMessage, T>(this, Message, input);
    }

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

    public InitializeContext<T> CreateMessageContext<T>(T message)
        where T : class
    {
        return new DynamicInitializeContext<T>(this, message);
    }
}


public class DynamicInitializeContext<TMessage, TInput> :
    DynamicInitializeContext<TMessage>,
    InitializeContext<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    public DynamicInitializeContext(InitializeContext context, TMessage message, TInput input)
        : base(context, message)
    {
        HasInput = (Input = input) != null;
    }

    public bool HasInput { get; }
    public TInput Input { get; } = null!;
}
