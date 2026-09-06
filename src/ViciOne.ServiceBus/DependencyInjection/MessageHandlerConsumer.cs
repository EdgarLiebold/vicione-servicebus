using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Consumes message handler messages.</summary>
/// <typeparam name="T">The value type.</typeparam>
[HandlerConsumerAdapter]
public class MessageHandlerConsumer<T> :
    IConsumer<T>
    where T : class
{
    readonly Func<ConsumeContext<T>, Task> _handler;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="method">The method.</param>
    public MessageHandlerConsumer(MessageHandlerMethod<T> method)
    {
        _handler = method.Handler;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeAsync(ConsumeContext<T> context)
    {
        return _handler(context);
    }
}


/// <summary>Consumes message handler messages.</summary>
/// <typeparam name="T">The value type.</typeparam>
/// <typeparam name="T1">The 1 type.</typeparam>
[HandlerConsumerAdapter]
public class MessageHandlerConsumer<T, T1> :
    IConsumer<T>
    where T : class
    where T1 : class
{
    readonly T1 _arg1;
    readonly Func<ConsumeContext<T>, T1, Task> _handler;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="method">The method.</param>
    /// <param name="arg1">The arg1.</param>
    public MessageHandlerConsumer(MessageHandlerMethod<T, T1> method, T1 arg1)
    {
        _handler = method.Handler;

        _arg1 = arg1;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeAsync(ConsumeContext<T> context)
    {
        return _handler(context, _arg1);
    }
}


/// <summary>Consumes message handler messages.</summary>
/// <typeparam name="T">The value type.</typeparam>
/// <typeparam name="T1">The 1 type.</typeparam>
/// <typeparam name="T2">The 2 type.</typeparam>
[HandlerConsumerAdapter]
public class MessageHandlerConsumer<T, T1, T2> :
    IConsumer<T>
    where T : class
    where T1 : class
    where T2 : class
{
    readonly T1 _arg1;
    readonly T2 _arg2;
    readonly Func<ConsumeContext<T>, T1, T2, Task> _handler;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="method">The method.</param>
    /// <param name="arg1">The arg1.</param>
    /// <param name="arg2">The arg2.</param>
    public MessageHandlerConsumer(MessageHandlerMethod<T, T1, T2> method, T1 arg1, T2 arg2)
    {
        _handler = method.Handler;

        _arg1 = arg1;
        _arg2 = arg2;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeAsync(ConsumeContext<T> context)
    {
        return _handler(context, _arg1, _arg2);
    }
}


/// <summary>Consumes message handler messages.</summary>
/// <typeparam name="T">The value type.</typeparam>
/// <typeparam name="T1">The 1 type.</typeparam>
/// <typeparam name="T2">The 2 type.</typeparam>
/// <typeparam name="T3">The 3 type.</typeparam>
[HandlerConsumerAdapter]
public class MessageHandlerConsumer<T, T1, T2, T3> :
    IConsumer<T>
    where T : class
    where T1 : class
    where T2 : class
    where T3 : class
{
    readonly T1 _arg1;
    readonly T2 _arg2;
    readonly T3 _arg3;
    readonly Func<ConsumeContext<T>, T1, T2, T3, Task> _handler;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="method">The method.</param>
    /// <param name="arg1">The arg1.</param>
    /// <param name="arg2">The arg2.</param>
    /// <param name="arg3">The arg3.</param>
    public MessageHandlerConsumer(MessageHandlerMethod<T, T1, T2, T3> method, T1 arg1, T2 arg2, T3 arg3)
    {
        _handler = method.Handler;

        _arg1 = arg1;
        _arg2 = arg2;
        _arg3 = arg3;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeAsync(ConsumeContext<T> context)
    {
        return _handler(context, _arg1, _arg2, _arg3);
    }
}
