using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

public class ToNullablePropertyConverter<TResult> :
    IPropertyConverter<TResult?, TResult>
    where TResult : struct
{
    public Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TResult input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<TResult?>(cancellationToken); return Task.FromResult<TResult?>(input);
    }
}


public class ToNullablePropertyConverter<TResult, TInput> :
    IPropertyConverter<TResult?, TInput>
    where TResult : struct
{
    readonly IPropertyConverter<TResult, TInput> _converter;

    public ToNullablePropertyConverter(IPropertyConverter<TResult, TInput> converter)
    {
        _converter = converter;
    }

    public Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TInput? input, CancellationToken cancellationToken = default)
        where T : class
    {
        Task<TResult> resultTask = _converter.ConvertAsync(context, input, cancellationToken: cancellationToken);
        if (resultTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<TResult?>(resultTask.Result);

        async Task<TResult?> ConvertAsync()
        {
            var result = await resultTask.ConfigureAwait(false);

            return result;
        }

        return ConvertAsync();
    }
}
