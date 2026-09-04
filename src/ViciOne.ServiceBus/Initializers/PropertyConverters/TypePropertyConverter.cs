using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>
/// Calls the property type converter, returning either the result or default.
/// </summary>
/// <typeparam name="TResult"></typeparam>
/// <typeparam name="TInput"></typeparam>
public class TypePropertyConverter<TResult, TInput> :
    IPropertyConverter<TResult, TInput>
{
    readonly ITypeConverter<TResult, TInput> _converter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="converter">The converter value.</param>
    public TypePropertyConverter(ITypeConverter<TResult, TInput> converter)
    {
        _converter = converter;
    }

    Task<TResult?> IPropertyConverter<TResult, TInput>.ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input, CancellationToken cancellationToken)
    {
        return _converter.TryConvert(input, out var result)
            ? Task.FromResult<TResult?>(result)
            : TaskResults.DefaultAsync<TResult>(cancellationToken: cancellationToken);
    }
}
