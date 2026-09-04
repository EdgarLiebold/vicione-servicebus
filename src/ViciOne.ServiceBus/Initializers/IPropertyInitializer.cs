using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>
/// A message initializer that uses the input
/// </summary>
/// <typeparam name="TMessage">The message type</typeparam>
/// <typeparam name="TInput">The input type</typeparam>
public interface IPropertyInitializer<in TMessage, in TInput>
    where TMessage : class
    where TInput : class
{
    Task ApplyAsync(InitializeContext<TMessage, TInput> context, CancellationToken cancellationToken = default);
}


/// <summary>
/// A message initializer that doesn't use the input
/// </summary>
/// <typeparam name="TMessage">The message type</typeparam>
public interface IPropertyInitializer<in TMessage>
    where TMessage : class
{
    /// <summary>
    /// Apply the initializer to the message
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task ApplyAsync(InitializeContext<TMessage> context, CancellationToken cancellationToken = default);
}
