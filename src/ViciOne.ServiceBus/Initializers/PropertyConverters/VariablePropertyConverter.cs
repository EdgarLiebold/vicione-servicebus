using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

public class VariablePropertyConverter<TResult, TVariable> :
    IPropertyConverter<TResult, TVariable>
    where TVariable : class, IInitializerVariable<TResult>
{
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


public class VariablePropertyConverter<TResult, TVariable, TValue> :
    IPropertyConverter<TResult, TVariable>
    where TVariable : class, IInitializerVariable<TValue>
{
    readonly IPropertyConverter<TResult, TValue> _propertyConverter;

    public VariablePropertyConverter(IPropertyConverter<TResult, TValue> propertyConverter)
    {
        _propertyConverter = propertyConverter;
    }

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
