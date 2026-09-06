using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Converts from nullable property values.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
public class FromNullablePropertyConverter<TResult> :
    IPropertyConverter<TResult, TResult?>
    where TResult : struct
{
    Task<TResult> IPropertyConverter<TResult, TResult?>.ConvertAsync<T>(InitializeContext<T> context, TResult? input, CancellationToken cancellationToken)
    {
        return Task.FromResult(input ?? default);
    }
}


/// <summary>Converts from nullable property values.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class FromNullablePropertyConverter<TResult, TInput> :
    IPropertyConverter<TResult, TInput?>
    where TInput : struct
{
    readonly IPropertyConverter<TResult, TInput> _converter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="converter">The converter.</param>
    public FromNullablePropertyConverter(IPropertyConverter<TResult, TInput> converter)
    {
        _converter = converter;
    }

    Task<TResult?> IPropertyConverter<TResult, TInput?>.ConvertAsync<T>(InitializeContext<T> context, TInput? input, CancellationToken cancellationToken)
    {
        return _converter.ConvertAsync(context, input ?? default, cancellationToken: cancellationToken);
    }
}
