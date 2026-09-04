using System;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>
/// Message initialization context, which includes the message being initialized and the input
/// being used to initialize the message properties.
/// </summary>
/// <typeparam name="TMessage">The message type</typeparam>
/// <typeparam name="TInput">The input type</typeparam>
public interface InitializeContext<out TMessage, out TInput> :
    InitializeContext<TMessage>
    where TMessage : class
    where TInput : class
{
    /// <summary>
    /// If true, the input is present, otherwise it equals <i>default</i>.
    /// </summary>
    bool HasInput { get; }

    /// <summary>
    /// Gets the input value.
    /// </summary>
    TInput Input { get; }
}


/// <summary>
/// The context of the message being initialized
/// </summary>
/// <typeparam name="TMessage">The message type</typeparam>
public interface InitializeContext<out TMessage> :
    InitializeContext
    where TMessage : class
{
    /// <summary>
    /// Gets the message type value.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    /// The message being initialized
    /// </summary>
    TMessage Message { get; }

    /// <summary>
    /// Creates input context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="input">The input value.</param>
    /// <returns>The result of the operation.</returns>
    InitializeContext<TMessage, T> CreateInputContext<T>(T input)
        where T : class;
}


/// <summary>
/// Defines the contract for initialize context.
/// </summary>
public interface InitializeContext :
    PipeContext
{
    /// <summary>
    /// how deep this context is within the object graph
    /// </summary>
    int Depth { get; }

    /// <summary>
    /// the parent initialize context, which is valid if the type is being initialized
    /// within another type
    /// </summary>
    InitializeContext? Parent { get; }

    /// <summary>
    /// Return the closest parent context for the specified type, if present
    /// </summary>
    /// <param name="parentContext"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    bool TryGetParent<T>([NotNullWhen(true)] out InitializeContext<T>? parentContext)
        where T : class;

    /// <summary>
    /// Creates message context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    InitializeContext<T> CreateMessageContext<T>(T message)
        where T : class;
}
