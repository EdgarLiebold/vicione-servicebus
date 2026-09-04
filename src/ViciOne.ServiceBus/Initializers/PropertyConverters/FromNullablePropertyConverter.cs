using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

public class FromNullablePropertyConverter<TResult> :
    IPropertyConverter<TResult, TResult?>
    where TResult : struct
{
    Task<TResult> IPropertyConverter<TResult, TResult?>.ConvertAsync<T>(InitializeContext<T> context, TResult? input, CancellationToken cancellationToken)
    {
        return Task.FromResult(input ?? default);
    }
}


public class FromNullablePropertyConverter<TResult, TInput> :
    IPropertyConverter<TResult, TInput?>
    where TInput : struct
{
    readonly IPropertyConverter<TResult, TInput> _converter;

    public FromNullablePropertyConverter(IPropertyConverter<TResult, TInput> converter)
    {
        _converter = converter;
    }

    Task<TResult?> IPropertyConverter<TResult, TInput?>.ConvertAsync<T>(InitializeContext<T> context, TInput? input, CancellationToken cancellationToken)
    {
        return _converter.ConvertAsync(context, input ?? default, cancellationToken: cancellationToken);
    }
}
