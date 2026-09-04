using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>
/// Defines the contract for initializer variable.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IInitializerVariable<T>
{
    /// <summary>
    /// Gets value.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<T> GetValueAsync<TMessage>(InitializeContext<TMessage> context, CancellationToken cancellationToken = default)
        where TMessage : class;
}
