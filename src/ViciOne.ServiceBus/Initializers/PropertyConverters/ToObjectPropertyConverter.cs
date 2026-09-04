using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

public class ToObjectPropertyConverter<TInput> :
    IPropertyConverter<object, TInput>
{
    public Task<object?> ConvertAsync<T>(InitializeContext<T> context, TInput? input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<object?>(cancellationToken); return Task.FromResult<object?>(input);
    }
}
