using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Initialize a message header.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public interface IHeaderInitializer<in TMessage, in TInput>
    where TMessage : class
    where TInput : class
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="sendContext">The send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default);
}


/// <summary>Initialize a message header.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IHeaderInitializer<in TMessage>
    where TMessage : class
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="sendContext">The send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ApplyAsync(InitializeContext<TMessage> context, SendContext sendContext, CancellationToken cancellationToken = default);
}
