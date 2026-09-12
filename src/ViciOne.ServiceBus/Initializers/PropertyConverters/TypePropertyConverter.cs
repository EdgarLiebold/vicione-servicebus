using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Adapts a synchronous type converter to the asynchronous property-converter contract.</summary>
/// <typeparam name="TResult">The target property type.</typeparam>
/// <typeparam name="TInput">The source value type.</typeparam>
internal sealed class TypePropertyConverter<TResult, TInput> :
    IPropertyConverter<TResult, TInput>
{
    readonly ITypeConverter<TResult, TInput> _converter;

    /// <summary>Creates an adapter for <paramref name="converter"/>.</summary>
    /// <param name="converter">The synchronous converter used for the property value.</param>
    public TypePropertyConverter(ITypeConverter<TResult, TInput> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    Task<TResult?> IPropertyConverter<TResult, TInput>.ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<TResult?>(cancellationToken);

        return _converter.TryConvert(input, out var result)
            ? Task.FromResult<TResult?>(result)
            : TaskResults.DefaultAsync<TResult>(cancellationToken: cancellationToken);
    }
}
