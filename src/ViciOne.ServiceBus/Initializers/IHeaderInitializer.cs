using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>
/// Initialize a message header
/// </summary>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TInput"></typeparam>
public interface IHeaderInitializer<in TMessage, in TInput>
    where TMessage : class
    where TInput : class
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="sendContext">The send context value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default);
}


/// <summary>
/// Initialize a message header
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface IHeaderInitializer<in TMessage>
    where TMessage : class
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="sendContext">The send context value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task ApplyAsync(InitializeContext<TMessage> context, SendContext sendContext, CancellationToken cancellationToken = default);
}
