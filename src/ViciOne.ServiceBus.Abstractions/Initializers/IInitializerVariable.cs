using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Defines the operations required by initializer variable.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IInitializerVariable<T>
{
    /// <summary>Gets value.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<T> GetValueAsync<TMessage>(InitializeContext<TMessage> context, CancellationToken cancellationToken = default)
        where TMessage : class;
}
