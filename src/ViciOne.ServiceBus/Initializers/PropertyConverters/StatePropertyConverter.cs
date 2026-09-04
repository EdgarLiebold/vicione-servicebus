using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

public class StatePropertyConverter<TInstance> :
    IPropertyConverter<string, State<TInstance>>
    where TInstance : class, SagaStateMachineInstance
{
    public Task<string?> ConvertAsync<T>(InitializeContext<T> context, State<TInstance>? input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<string?>(cancellationToken); return Task.FromResult(input?.Name);
    }
}


public class StatePropertyConverter<TResult, TInstance> :
    IPropertyConverter<TResult, State<TInstance>>
    where TInstance : class, SagaStateMachineInstance
{
    readonly IPropertyConverter<TResult, string> _propertyConverter;

    public StatePropertyConverter(IPropertyConverter<TResult, string> propertyConverter)
    {
        _propertyConverter = propertyConverter;
    }

    public Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, State<TInstance>? input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (input == default)
            return Task.FromResult<TResult?>(default);

        var name = input?.Name;

        return _propertyConverter.ConvertAsync(context, name, cancellationToken: cancellationToken);
    }
}
