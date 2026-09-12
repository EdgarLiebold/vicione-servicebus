using System.Threading.Tasks;
namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Exposes a value-type provider through its nullable property contract.</summary>
/// <typeparam name="TInput">The input object type.</typeparam>
/// <typeparam name="TProperty">The underlying value type.</typeparam>
internal sealed class ToNullablePropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty?>
    where TInput : class
    where TProperty : struct
{
    readonly IPropertyProvider<TInput, TProperty> _provider;

    /// <summary>Creates a nullable adapter for <paramref name="provider" />.</summary>
    /// <param name="provider">The non-nullable value provider.</param>
    public ToNullablePropertyProvider(IPropertyProvider<TInput, TProperty> provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Returns the supplied value in nullable form.</summary>
    /// <typeparam name="T">The message contract being initialized.</typeparam>
    /// <param name="context">The message and input object used for value resolution.</param>
    /// <param name="cancellationToken">The token that cancels value resolution.</param>
    /// <returns>A task containing the resolved value in nullable form.</returns>
    public async Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.HasInput)
            return default;

        Task<TProperty> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The property provider returned null.");
        return await propertyTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}
