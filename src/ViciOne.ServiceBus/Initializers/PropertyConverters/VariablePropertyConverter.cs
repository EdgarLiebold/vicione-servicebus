using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Resolves an initializer variable as a property value.</summary>
/// <typeparam name="TResult">The variable value exposed as the property result.</typeparam>
/// <typeparam name="TVariable">The variable type.</typeparam>
/// <remarks>An accepted variable resolution is observed to its original terminal outcome.</remarks>
internal sealed class VariablePropertyConverter<TResult, TVariable> :
    IPropertyConverter<TResult, TVariable>
    where TVariable : class, IInitializerVariable<TResult>
{
    /// <inheritdoc />
    public async Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TVariable? input, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (input == null)
            return default;

        Task<TResult> valueTask = input.GetValueAsync(context, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The initializer variable returned null.");
        return await valueTask.ConfigureAwait(false);
    }
}


/// <summary>Resolves an initializer variable and converts its value.</summary>
/// <typeparam name="TResult">The converted property value type.</typeparam>
/// <typeparam name="TVariable">The variable type.</typeparam>
/// <typeparam name="TValue">The value resolved by the initializer variable.</typeparam>
/// <remarks>Accepted variable resolution and value conversion are each observed to their original terminal outcome.</remarks>
internal sealed class VariablePropertyConverter<TResult, TVariable, TValue> :
    IPropertyConverter<TResult, TVariable>
    where TVariable : class, IInitializerVariable<TValue>
{
    readonly IPropertyConverter<TResult, TValue> _propertyConverter;

    /// <summary>Creates a variable converter backed by <paramref name="propertyConverter"/>.</summary>
    /// <param name="propertyConverter">The converter applied to the resolved variable value.</param>
    public VariablePropertyConverter(IPropertyConverter<TResult, TValue> propertyConverter)
    {
        _propertyConverter = propertyConverter ?? throw new ArgumentNullException(nameof(propertyConverter));
    }

    /// <inheritdoc />
    public async Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TVariable? input, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (input == default)
            return default;

        Task<TValue> inputTask = input.GetValueAsync(context, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The initializer variable returned null.");
        var value = await inputTask.ConfigureAwait(false);
        Task<TResult?> conversionTask = _propertyConverter.ConvertAsync(context, value, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The property converter returned null.");
        return await conversionTask.ConfigureAwait(false);
    }
}
