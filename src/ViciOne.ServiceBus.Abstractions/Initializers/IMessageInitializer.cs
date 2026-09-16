using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Creates and populates messages from runtime input objects.</summary>
/// <typeparam name="TMessage">The message contract produced by the initializer.</typeparam>
/// <remarks>Required arguments are validated before initialization starts. Cancellation is checked before
/// each input application and forwarded to its callbacks. An accepted callback batch is observed to its
/// original completion; cancellation does not abandon callbacks that are still modifying the message.
/// Ordinary callback failures take precedence over canceled callbacks in the same batch.</remarks>
public interface IMessageInitializer<TMessage>
    where TMessage : class
{
    /// <summary>Creates an unpopulated message context that inherits the supplied pipeline context.</summary>
    /// <param name="context">The pipeline context that supplies payloads and cancellation state.</param>
    /// <returns>A context containing a newly created message.</returns>
    InitializeContext<TMessage> Create(PipeContext context);

    /// <summary>Creates an unpopulated message context with the supplied cancellation token.</summary>
    /// <param name="cancellationToken">The token exposed by the new initialization context.</param>
    /// <returns>A context containing a newly created message.</returns>
    InitializeContext<TMessage> Create(CancellationToken cancellationToken);

    /// <summary>Creates and populates a message from an input object.</summary>
    /// <param name="input">The object whose compatible values populate the message.</param>
    /// <param name="cancellationToken">The token that cancels initialization.</param>
    /// <returns>A task containing the populated message context.</returns>
    Task<InitializeContext<TMessage>> InitializeAsync(object input, CancellationToken cancellationToken);

    /// <summary>Applies an additional input object to an existing message context.</summary>
    /// <param name="context">The message context to populate.</param>
    /// <param name="input">The object whose compatible values populate the message.</param>
    /// <param name="cancellationToken">The token that cancels initialization.</param>
    /// <returns>A task containing the populated message context.</returns>
    Task<InitializeContext<TMessage>> InitializeAsync(InitializeContext<TMessage> context, object input, CancellationToken cancellationToken = default);

    /// <summary>Creates a message and prepares the send pipe that applies initialized headers.</summary>
    /// <param name="context">The pipeline context inherited by message initialization.</param>
    /// <param name="input">The object whose compatible values populate the message.</param>
    /// <param name="pipe">Additional send-pipeline stages to include.</param>
    /// <param name="cancellationToken">The token that cancels initialization.</param>
    /// <returns>A task containing the populated message and its send pipe.</returns>
    Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(PipeContext context, object input, IPipe<SendContext<TMessage>>? pipe = null, CancellationToken cancellationToken = default);

    /// <summary>Creates a message, applies multiple input objects, and prepares its send pipe.</summary>
    /// <param name="context">The pipeline context inherited by message initialization.</param>
    /// <param name="input">The primary object applied after the additional inputs.</param>
    /// <param name="moreInputs">Additional input objects applied in array order before <paramref name="input"/>; null entries are ignored.</param>
    /// <param name="pipe">Additional send-pipeline stages to include.</param>
    /// <param name="cancellationToken">The token that cancels initialization.</param>
    /// <returns>A task containing the populated message and its send pipe.</returns>
    Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(PipeContext context, object input, object?[] moreInputs, IPipe<SendContext<TMessage>>? pipe = null, CancellationToken cancellationToken = default);

    /// <summary>Creates a message and prepares its send pipe without inheriting another pipeline context.</summary>
    /// <param name="input">The object whose compatible values populate the message.</param>
    /// <param name="pipe">The send-pipeline stages associated with the initialized message.</param>
    /// <param name="cancellationToken">The token that cancels initialization.</param>
    /// <returns>A task containing the populated message and its send pipe.</returns>
    Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(object input, IPipe<SendContext<TMessage>> pipe, CancellationToken cancellationToken);
}
