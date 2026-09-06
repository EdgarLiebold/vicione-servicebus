using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Invokes the registered method for message handler.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessageHandlerMethod<TMessage>
    where TMessage : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="handler">The handler.</param>
    public MessageHandlerMethod(Func<ConsumeContext<TMessage>, Task> handler)
    {
        Handler = handler;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="handler">The handler.</param>
    public MessageHandlerMethod(Func<TMessage, Task> handler)
    {
        Handler = context => handler(context.Message);
    }

    /// <summary>Gets the handler.</summary>
    public Func<ConsumeContext<TMessage>, Task> Handler { get; }
}


/// <summary>Invokes the registered method for message handler.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="T1">The 1 type.</typeparam>
public class MessageHandlerMethod<TMessage, T1>
    where TMessage : class
    where T1 : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="handler">The handler.</param>
    public MessageHandlerMethod(Func<ConsumeContext<TMessage>, T1, Task> handler)
    {
        Handler = handler;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="handler">The handler.</param>
    public MessageHandlerMethod(Func<TMessage, T1, Task> handler)
    {
        Handler = (context, arg1) => handler(context.Message, arg1);
    }

    /// <summary>Gets the handler.</summary>
    public Func<ConsumeContext<TMessage>, T1, Task> Handler { get; }
}


/// <summary>Invokes the registered method for message handler.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="T1">The 1 type.</typeparam>
/// <typeparam name="T2">The 2 type.</typeparam>
public class MessageHandlerMethod<TMessage, T1, T2>
    where TMessage : class
    where T1 : class
    where T2 : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="handler">The handler.</param>
    public MessageHandlerMethod(Func<ConsumeContext<TMessage>, T1, T2, Task> handler)
    {
        Handler = handler;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="handler">The handler.</param>
    public MessageHandlerMethod(Func<TMessage, T1, T2, Task> handler)
    {
        Handler = (context, arg1, arg2) => handler(context.Message, arg1, arg2);
    }

    /// <summary>Gets the handler.</summary>
    public Func<ConsumeContext<TMessage>, T1, T2, Task> Handler { get; }
}


/// <summary>Invokes the registered method for message handler.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="T1">The 1 type.</typeparam>
/// <typeparam name="T2">The 2 type.</typeparam>
/// <typeparam name="T3">The 3 type.</typeparam>
public class MessageHandlerMethod<TMessage, T1, T2, T3>
    where TMessage : class
    where T1 : class
    where T2 : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="handler">The handler.</param>
    public MessageHandlerMethod(Func<ConsumeContext<TMessage>, T1, T2, T3, Task> handler)
    {
        Handler = handler;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="handler">The handler.</param>
    public MessageHandlerMethod(Func<TMessage, T1, T2, T3, Task> handler)
    {
        Handler = (context, arg1, arg2, arg3) => handler(context.Message, arg1, arg2, arg3);
    }

    /// <summary>Gets the handler.</summary>
    public Func<ConsumeContext<TMessage>, T1, T2, T3, Task> Handler { get; }
}
