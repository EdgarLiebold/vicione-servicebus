using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Converts a nullable value-type provider to a non-nullable provider.</summary>
/// <typeparam name="TInput">The input object type.</typeparam>
/// <typeparam name="TProperty">The underlying value type.</typeparam>
internal sealed class FromNullablePropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
    where TProperty : struct
{
    readonly IPropertyProvider<TInput, TProperty?> _provider;

    /// <summary>Creates a provider that unwraps values returned by <paramref name="provider" />.</summary>
    /// <param name="provider">The nullable value provider.</param>
    public FromNullablePropertyProvider(IPropertyProvider<TInput, TProperty?> provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Forwards caller cancellation and observes the accepted value resolution to completion.</summary>
    /// <typeparam name="T">The message contract being initialized.</typeparam>
    /// <param name="context">The message and input object used for value resolution.</param>
    /// <param name="cancellationToken">The token forwarded to value resolution.</param>
    /// <returns>A task containing the resolved value or its default when absent.</returns>
    public async Task<TProperty> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.HasInput)
            return default;

        Task<TProperty?> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The nullable property provider returned null.");
        return await propertyTask.ConfigureAwait(false) ?? default;
    }
}
