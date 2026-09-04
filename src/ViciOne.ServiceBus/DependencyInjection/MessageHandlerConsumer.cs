using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a message handler consumer implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
[HandlerConsumerAdapter]
public class MessageHandlerConsumer<T> :
    IConsumer<T>
    where T : class
{
    readonly Func<ConsumeContext<T>, Task> _handler;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="method">The method value.</param>
    public MessageHandlerConsumer(MessageHandlerMethod<T> method)
    {
        _handler = method.Handler;
    }

    /// <summary>
    /// Consumes the message provided by the context.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task ConsumeAsync(ConsumeContext<T> context)
    {
        return _handler(context);
    }
}


/// <summary>
/// Provides a message handler consumer implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
/// <typeparam name="T1">The t1 type.</typeparam>
[HandlerConsumerAdapter]
public class MessageHandlerConsumer<T, T1> :
    IConsumer<T>
    where T : class
    where T1 : class
{
    readonly T1 _arg1;
    readonly Func<ConsumeContext<T>, T1, Task> _handler;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="method">The method value.</param>
    /// <param name="arg1">The arg1 value.</param>
    public MessageHandlerConsumer(MessageHandlerMethod<T, T1> method, T1 arg1)
    {
        _handler = method.Handler;

        _arg1 = arg1;
    }

    /// <summary>
    /// Consumes the message provided by the context.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task ConsumeAsync(ConsumeContext<T> context)
    {
        return _handler(context, _arg1);
    }
}


/// <summary>
/// Provides a message handler consumer implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
/// <typeparam name="T1">The t1 type.</typeparam>
/// <typeparam name="T2">The t2 type.</typeparam>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="method">The method value.</param>
    /// <param name="arg1">The arg1 value.</param>
    /// <param name="arg2">The arg2 value.</param>
    public MessageHandlerConsumer(MessageHandlerMethod<T, T1, T2> method, T1 arg1, T2 arg2)
    {
        _handler = method.Handler;

        _arg1 = arg1;
        _arg2 = arg2;
    }

    /// <summary>
    /// Consumes the message provided by the context.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task ConsumeAsync(ConsumeContext<T> context)
    {
        return _handler(context, _arg1, _arg2);
    }
}


/// <summary>
/// Provides a message handler consumer implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
/// <typeparam name="T1">The t1 type.</typeparam>
/// <typeparam name="T2">The t2 type.</typeparam>
/// <typeparam name="T3">The t3 type.</typeparam>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="method">The method value.</param>
    /// <param name="arg1">The arg1 value.</param>
    /// <param name="arg2">The arg2 value.</param>
    /// <param name="arg3">The arg3 value.</param>
    public MessageHandlerConsumer(MessageHandlerMethod<T, T1, T2, T3> method, T1 arg1, T2 arg2, T3 arg3)
    {
        _handler = method.Handler;

        _arg1 = arg1;
        _arg2 = arg2;
        _arg3 = arg3;
    }

    /// <summary>
    /// Consumes the message provided by the context.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task ConsumeAsync(ConsumeContext<T> context)
    {
        return _handler(context, _arg1, _arg2, _arg3);
    }
}
