using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Converts state property values.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
public class StatePropertyConverter<TInstance> :
    IPropertyConverter<string, State<TInstance>>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<string?> ConvertAsync<T>(InitializeContext<T> context, State<TInstance>? input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<string?>(cancellationToken);

        return Task.FromResult(input?.Name);
    }
}


/// <summary>Converts state property values.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="TInstance">The instance type.</typeparam>
public class StatePropertyConverter<TResult, TInstance> :
    IPropertyConverter<TResult, State<TInstance>>
    where TInstance : class, SagaStateMachineInstance
{
    readonly IPropertyConverter<TResult, string> _propertyConverter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="propertyConverter">The property converter.</param>
    public StatePropertyConverter(IPropertyConverter<TResult, string> propertyConverter)
    {
        _propertyConverter = propertyConverter;
    }

    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, State<TInstance>? input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (input == default)
            return Task.FromResult<TResult?>(default);

        var name = input?.Name;

        return _propertyConverter.ConvertAsync(context, name, cancellationToken: cancellationToken);
    }
}
