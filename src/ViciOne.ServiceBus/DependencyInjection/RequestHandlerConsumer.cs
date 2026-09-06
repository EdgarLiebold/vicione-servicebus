using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Consumes request handler messages.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
[HandlerConsumerAdapter]
public class RequestHandlerConsumer<TMessage, TResponse> :
    IConsumer<TMessage>
    where TMessage : class
    where TResponse : class
{
    readonly Func<ConsumeContext<TMessage>, Task<TResponse>> _handler;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="method">The method.</param>
    public RequestHandlerConsumer(RequestHandlerMethod<TMessage, TResponse> method)
    {
        _handler = method.Handler;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<TMessage> context)
    {
        var response = await _handler(context).ConfigureAwait(false);

        if (response != null)
            await context.RespondAsync(response).ConfigureAwait(false);
    }
}


/// <summary>Consumes request handler messages.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="T1">The 1 type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
[HandlerConsumerAdapter]
public class RequestHandlerConsumer<TMessage, T1, TResponse> :
    IConsumer<TMessage>
    where TMessage : class
    where TResponse : class
    where T1 : class
{
    readonly T1 _arg1;
    readonly Func<ConsumeContext<TMessage>, T1, Task<TResponse>> _handler;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="method">The method.</param>
    /// <param name="arg1">The arg1.</param>
    public RequestHandlerConsumer(RequestHandlerMethod<TMessage, T1, TResponse> method, T1 arg1)
    {
        _arg1 = arg1;
        _handler = method.Handler;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<TMessage> context)
    {
        var response = await _handler(context, _arg1).ConfigureAwait(false);

        if (response != null)
            await context.RespondAsync(response).ConfigureAwait(false);
    }
}


/// <summary>Consumes request handler messages.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="T1">The 1 type.</typeparam>
/// <typeparam name="T2">The 2 type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
[HandlerConsumerAdapter]
public class RequestHandlerConsumer<TMessage, T1, T2, TResponse> :
    IConsumer<TMessage>
    where TMessage : class
    where TResponse : class
    where T1 : class
    where T2 : class
{
    readonly T1 _arg1;
    readonly T2 _arg2;
    readonly Func<ConsumeContext<TMessage>, T1, T2, Task<TResponse>> _handler;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="method">The method.</param>
    /// <param name="arg1">The arg1.</param>
    /// <param name="arg2">The arg2.</param>
    public RequestHandlerConsumer(RequestHandlerMethod<TMessage, T1, T2, TResponse> method, T1 arg1, T2 arg2)
    {
        _arg1 = arg1;
        _arg2 = arg2;
        _handler = method.Handler;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<TMessage> context)
    {
        var response = await _handler(context, _arg1, _arg2).ConfigureAwait(false);

        if (response != null)
            await context.RespondAsync(response).ConfigureAwait(false);
    }
}


/// <summary>Consumes request handler messages.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="T1">The 1 type.</typeparam>
/// <typeparam name="T2">The 2 type.</typeparam>
/// <typeparam name="T3">The 3 type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
[HandlerConsumerAdapter]
public class RequestHandlerConsumer<TMessage, T1, T2, T3, TResponse> :
    IConsumer<TMessage>
    where TMessage : class
    where TResponse : class
    where T1 : class
    where T2 : class
    where T3 : class
{
    readonly T1 _arg1;
    readonly T2 _arg2;
    readonly T3 _arg3;
    readonly Func<ConsumeContext<TMessage>, T1, T2, T3, Task<TResponse>> _handler;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="method">The method.</param>
    /// <param name="arg1">The arg1.</param>
    /// <param name="arg2">The arg2.</param>
    /// <param name="arg3">The arg3.</param>
    public RequestHandlerConsumer(RequestHandlerMethod<TMessage, T1, T2, T3, TResponse> method, T1 arg1, T2 arg2, T3 arg3)
    {
        _arg1 = arg1;
        _arg2 = arg2;
        _arg3 = arg3;
        _handler = method.Handler;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<TMessage> context)
    {
        var response = await _handler(context, _arg1, _arg2, _arg3).ConfigureAwait(false);

        if (response != null)
            await context.RespondAsync(response).ConfigureAwait(false);
    }
}
