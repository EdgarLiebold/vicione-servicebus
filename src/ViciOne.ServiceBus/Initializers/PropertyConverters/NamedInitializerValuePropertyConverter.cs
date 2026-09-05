using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Initializers;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

sealed class NamedInitializerValuePropertyConverter<TValue> :
    IPropertyConverter<string, TValue>
    where TValue : class, INamedInitializerValue
{
    public Task<string?> ConvertAsync<T>(InitializeContext<T> context, TValue? input,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);

        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<string?>(cancellationToken)
            : Task.FromResult(input?.Name);
    }
}

sealed class NamedInitializerValuePropertyConverter<TResult, TValue> :
    IPropertyConverter<TResult, TValue>
    where TValue : class, INamedInitializerValue
{
    readonly IPropertyConverter<TResult, string> _nameConverter;

    public NamedInitializerValuePropertyConverter(IPropertyConverter<TResult, string> nameConverter)
    {
        _nameConverter = nameConverter ?? throw new ArgumentNullException(nameof(nameConverter));
    }

    public Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TValue? input,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);

        return input == null
            ? Task.FromResult<TResult?>(default)
            : _nameConverter.ConvertAsync(context, input.Name, cancellationToken);
    }
}
