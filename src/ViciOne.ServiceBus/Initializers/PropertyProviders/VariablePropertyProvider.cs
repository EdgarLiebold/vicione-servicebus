using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Resolves an initializer variable supplied by an input property.</summary>
/// <typeparam name="TInput">The input object type.</typeparam>
/// <typeparam name="TProperty">The initializer-variable type.</typeparam>
/// <typeparam name="TValue">The value resolved by the variable.</typeparam>
internal sealed class VariablePropertyProvider<TInput, TProperty, TValue> :
    IPropertyProvider<TInput, TValue>
    where TInput : class
    where TProperty : class, IInitializerVariable<TValue>
{
    readonly IPropertyProvider<TInput, TProperty> _provider;

    /// <summary>Creates a provider that resolves variables returned by <paramref name="provider"/>.</summary>
    /// <param name="provider">The provider that supplies the initializer variable.</param>
    public VariablePropertyProvider(IPropertyProvider<TInput, TProperty> provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Resolves the variable value with caller cancellation.</summary>
    /// <typeparam name="T">The message contract being initialized.</typeparam>
    /// <param name="context">The message and input object available to the variable.</param>
    /// <param name="cancellationToken">The token that cancels provider and variable resolution.</param>
    /// <returns>A task containing the variable's value, or the default value when no variable is available.</returns>
    public async Task<TValue?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.HasInput)
            return default;

        Task<TProperty?> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The variable property provider returned null.");
        TProperty? property = await propertyTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (property == null)
            return default;

        Task<TValue> valueTask = property.GetValueAsync(context, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The initializer variable returned null.");
        return await valueTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}
