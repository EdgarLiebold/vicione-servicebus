using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a message handler method implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageHandlerMethod<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public MessageHandlerMethod(Func<ConsumeContext<TMessage>, Task> handler)
    {
        Handler = handler;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public MessageHandlerMethod(Func<TMessage, Task> handler)
    {
        Handler = context => handler(context.Message);
    }

    /// <summary>
    /// Gets the handler value.
    /// </summary>
    public Func<ConsumeContext<TMessage>, Task> Handler { get; }
}


/// <summary>
/// Provides a message handler method implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="T1">The t1 type.</typeparam>
public class MessageHandlerMethod<TMessage, T1>
    where TMessage : class
    where T1 : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public MessageHandlerMethod(Func<ConsumeContext<TMessage>, T1, Task> handler)
    {
        Handler = handler;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public MessageHandlerMethod(Func<TMessage, T1, Task> handler)
    {
        Handler = (context, arg1) => handler(context.Message, arg1);
    }

    /// <summary>
    /// Gets the handler value.
    /// </summary>
    public Func<ConsumeContext<TMessage>, T1, Task> Handler { get; }
}


/// <summary>
/// Provides a message handler method implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="T1">The t1 type.</typeparam>
/// <typeparam name="T2">The t2 type.</typeparam>
public class MessageHandlerMethod<TMessage, T1, T2>
    where TMessage : class
    where T1 : class
    where T2 : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public MessageHandlerMethod(Func<ConsumeContext<TMessage>, T1, T2, Task> handler)
    {
        Handler = handler;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public MessageHandlerMethod(Func<TMessage, T1, T2, Task> handler)
    {
        Handler = (context, arg1, arg2) => handler(context.Message, arg1, arg2);
    }

    /// <summary>
    /// Gets the handler value.
    /// </summary>
    public Func<ConsumeContext<TMessage>, T1, T2, Task> Handler { get; }
}


/// <summary>
/// Provides a message handler method implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="T1">The t1 type.</typeparam>
/// <typeparam name="T2">The t2 type.</typeparam>
/// <typeparam name="T3">The t3 type.</typeparam>
public class MessageHandlerMethod<TMessage, T1, T2, T3>
    where TMessage : class
    where T1 : class
    where T2 : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public MessageHandlerMethod(Func<ConsumeContext<TMessage>, T1, T2, T3, Task> handler)
    {
        Handler = handler;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public MessageHandlerMethod(Func<TMessage, T1, T2, T3, Task> handler)
    {
        Handler = (context, arg1, arg2, arg3) => handler(context.Message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Gets the handler value.
    /// </summary>
    public Func<ConsumeContext<TMessage>, T1, T2, T3, Task> Handler { get; }
}
