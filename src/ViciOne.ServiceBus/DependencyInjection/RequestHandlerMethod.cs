using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a request handler method implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
public class RequestHandlerMethod<TMessage, TResponse>
    where TMessage : class
    where TResponse : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public RequestHandlerMethod(Func<ConsumeContext<TMessage>, Task<TResponse>> handler)
    {
        Handler = handler;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public RequestHandlerMethod(Func<TMessage, Task<TResponse>> handler)
    {
        Handler = context => handler(context.Message);
    }

    /// <summary>
    /// Gets the handler value.
    /// </summary>
    public Func<ConsumeContext<TMessage>, Task<TResponse>> Handler { get; }
}


/// <summary>
/// Provides a request handler method implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="T1">The t1 type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
public class RequestHandlerMethod<TMessage, T1, TResponse>
    where TMessage : class
    where T1 : class
    where TResponse : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public RequestHandlerMethod(Func<ConsumeContext<TMessage>, T1, Task<TResponse>> handler)
    {
        Handler = handler;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public RequestHandlerMethod(Func<TMessage, T1, Task<TResponse>> handler)
    {
        Handler = (context, arg1) => handler(context.Message, arg1);
    }

    /// <summary>
    /// Gets the handler value.
    /// </summary>
    public Func<ConsumeContext<TMessage>, T1, Task<TResponse>> Handler { get; }
}


/// <summary>
/// Provides a request handler method implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="T1">The t1 type.</typeparam>
/// <typeparam name="T2">The t2 type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
public class RequestHandlerMethod<TMessage, T1, T2, TResponse>
    where TMessage : class
    where T1 : class
    where T2 : class
    where TResponse : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public RequestHandlerMethod(Func<ConsumeContext<TMessage>, T1, T2, Task<TResponse>> handler)
    {
        Handler = handler;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public RequestHandlerMethod(Func<TMessage, T1, T2, Task<TResponse>> handler)
    {
        Handler = (context, arg1, arg2) => handler(context.Message, arg1, arg2);
    }

    /// <summary>
    /// Gets the handler value.
    /// </summary>
    public Func<ConsumeContext<TMessage>, T1, T2, Task<TResponse>> Handler { get; }
}


/// <summary>
/// Provides a request handler method implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="T1">The t1 type.</typeparam>
/// <typeparam name="T2">The t2 type.</typeparam>
/// <typeparam name="T3">The t3 type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
public class RequestHandlerMethod<TMessage, T1, T2, T3, TResponse>
    where TMessage : class
    where T1 : class
    where T2 : class
    where T3 : class
    where TResponse : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public RequestHandlerMethod(Func<ConsumeContext<TMessage>, T1, T2, T3, Task<TResponse>> handler)
    {
        Handler = handler;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public RequestHandlerMethod(Func<TMessage, T1, T2, T3, Task<TResponse>> handler)
    {
        Handler = (context, arg1, arg2, arg3) => handler(context.Message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Gets the handler value.
    /// </summary>
    public Func<ConsumeContext<TMessage>, T1, T2, T3, Task<TResponse>> Handler { get; }
}
