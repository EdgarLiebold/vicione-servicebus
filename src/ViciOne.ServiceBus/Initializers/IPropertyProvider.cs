using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>
/// Returns the property from the input
/// </summary>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TProperty"></typeparam>
public interface IPropertyProvider<in TInput, TProperty>
    where TInput : class
{
    /// <summary>
    /// Gets property.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class;
}
