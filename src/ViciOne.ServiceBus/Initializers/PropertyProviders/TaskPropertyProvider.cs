using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Exposes a property-provider operation as a task-valued property.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
internal sealed class TaskPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, Task<TProperty?>>
    where TInput : class
{
    readonly IPropertyProvider<TInput, TProperty> _provider;

    /// <summary>Creates a task-valued wrapper around <paramref name="provider"/>.</summary>
    /// <param name="provider">The provider whose operation becomes the property value.</param>
    public TaskPropertyProvider(IPropertyProvider<TInput, TProperty> provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Returns the underlying value-resolution task without awaiting it.</summary>
    /// <typeparam name="T">The message contract being initialized.</typeparam>
    /// <param name="context">The message and input object used for value resolution.</param>
    /// <param name="cancellationToken">The token forwarded to the underlying provider.</param>
    /// <returns>A completed outer task containing the value-resolution task.</returns>
    public Task<Task<TProperty?>?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<Task<TProperty?>?>(cancellationToken);

        Task<TProperty?> propertyTask = context.HasInput
            ? _provider.GetPropertyAsync(context, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("The property provider returned a null task.")
            : TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

        return Task.FromResult<Task<TProperty?>?>(propertyTask);
    }
}
