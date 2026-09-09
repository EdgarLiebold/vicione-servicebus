using System;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>
/// Exposes a message together with one input object used to populate its properties.
/// </summary>
/// <typeparam name="TMessage">The message contract being initialized.</typeparam>
/// <typeparam name="TInput">The input-object type.</typeparam>
public interface InitializeContext<out TMessage, out TInput> :
    InitializeContext<TMessage>
    where TMessage : class
    where TInput : class
{
    /// <summary>Gets whether an input object is available.</summary>
    bool HasInput { get; }

    /// <summary>Gets the input object.</summary>
    TInput Input { get; }
}


/// <summary>Exposes a message while its properties and outgoing headers are initialized.</summary>
/// <typeparam name="TMessage">The message contract being initialized.</typeparam>
public interface InitializeContext<out TMessage> :
    InitializeContext
    where TMessage : class
{
    /// <summary>Gets the message type.</summary>
    Type MessageType { get; }

    /// <summary>Gets the message being initialized.</summary>
    TMessage Message { get; }

    /// <summary>Creates a view that associates an input object with this message.</summary>
    /// <typeparam name="T">The input-object type.</typeparam>
    /// <param name="input">The input object to expose.</param>
    /// <returns>A context exposing both the message and input object.</returns>
    InitializeContext<TMessage, T> CreateInputContext<T>(T input)
        where T : class;
}


/// <summary>Exposes the pipeline state and object-graph position of message initialization.</summary>
public interface InitializeContext :
    PipeContext
{
    /// <summary>Gets the zero-based depth of this context in the initialized object graph.</summary>
    int Depth { get; }

    /// <summary>
    /// Gets the containing initialization context, or null for the root context.
    /// </summary>
    InitializeContext? Parent { get; }

    /// <summary>Finds the closest context whose message implements the requested contract.</summary>
    /// <typeparam name="T">The requested parent message contract.</typeparam>
    /// <param name="parentContext">Receives the matching context when one is available.</param>
    /// <returns><see langword="true"/> when a matching context is found; otherwise, <see langword="false"/>.</returns>
    bool TryGetParent<T>([NotNullWhen(true)] out InitializeContext<T>? parentContext)
        where T : class;

    /// <summary>Creates a child context for a message nested in the current object graph.</summary>
    /// <typeparam name="T">The nested message contract.</typeparam>
    /// <param name="message">The nested message instance.</param>
    /// <returns>A child initialization context for <paramref name="message"/>.</returns>
    InitializeContext<T> CreateMessageContext<T>(T message)
        where T : class;
}
