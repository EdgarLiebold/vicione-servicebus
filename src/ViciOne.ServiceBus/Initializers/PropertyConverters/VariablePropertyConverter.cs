using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Converts variable property values.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="TVariable">The variable type.</typeparam>
public class VariablePropertyConverter<TResult, TVariable> :
    IPropertyConverter<TResult, TVariable>
    where TVariable : class, IInitializerVariable<TResult>
{
    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TVariable? input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (input == null)
            return TaskResults.DefaultAsync<TResult>(cancellationToken: cancellationToken);

        return GetValueAsync(input);

        async Task<TResult?> GetValueAsync(TVariable variable)
        {
            return await variable.GetValueAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}


/// <summary>Converts variable property values.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="TVariable">The variable type.</typeparam>
/// <typeparam name="TValue">The value stored by the member.</typeparam>
public class VariablePropertyConverter<TResult, TVariable, TValue> :
    IPropertyConverter<TResult, TVariable>
    where TVariable : class, IInitializerVariable<TValue>
{
    readonly IPropertyConverter<TResult, TValue> _propertyConverter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="propertyConverter">The property converter.</param>
    public VariablePropertyConverter(IPropertyConverter<TResult, TValue> propertyConverter)
    {
        _propertyConverter = propertyConverter;
    }

    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TVariable? input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (input == default)
            return Task.FromResult<TResult?>(default);

        Task<TValue> inputTask = input.GetValueAsync(context, cancellationToken: cancellationToken);
        if (inputTask.Status == TaskStatus.RanToCompletion)
            return _propertyConverter.ConvertAsync(context, inputTask.Result, cancellationToken: cancellationToken);

        async Task<TResult?> ConvertAsync()
        {
            var value = await inputTask.ConfigureAwait(false);

            Task<TResult?> convertTask = _propertyConverter.ConvertAsync(context, value, cancellationToken: cancellationToken);
            if (convertTask.Status == TaskStatus.RanToCompletion)
                return convertTask.Result;

            return await convertTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }
}
