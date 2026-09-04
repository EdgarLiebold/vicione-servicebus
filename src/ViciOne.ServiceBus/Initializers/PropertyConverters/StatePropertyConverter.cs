using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>
/// Provides a state property converter implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
public class StatePropertyConverter<TInstance> :
    IPropertyConverter<string, State<TInstance>>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="input">The input value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<string?> ConvertAsync<T>(InitializeContext<T> context, State<TInstance>? input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<string?>(cancellationToken); return Task.FromResult(input?.Name);
    }
}


/// <summary>
/// Provides a state property converter implementation.
/// </summary>
/// <typeparam name="TResult">The t result type.</typeparam>
/// <typeparam name="TInstance">The t instance type.</typeparam>
public class StatePropertyConverter<TResult, TInstance> :
    IPropertyConverter<TResult, State<TInstance>>
    where TInstance : class, SagaStateMachineInstance
{
    readonly IPropertyConverter<TResult, string> _propertyConverter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="propertyConverter">The property converter value.</param>
    public StatePropertyConverter(IPropertyConverter<TResult, string> propertyConverter)
    {
        _propertyConverter = propertyConverter;
    }

    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="input">The input value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, State<TInstance>? input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (input == default)
            return Task.FromResult<TResult?>(default);

        var name = input?.Name;

        return _propertyConverter.ConvertAsync(context, name, cancellationToken: cancellationToken);
    }
}
