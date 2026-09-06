using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Converts to nullable property values.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
public class ToNullablePropertyConverter<TResult> :
    IPropertyConverter<TResult?, TResult>
    where TResult : struct
{
    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    public Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TResult input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<TResult?>(cancellationToken); return Task.FromResult<TResult?>(input);
    }
}


/// <summary>Converts to nullable property values.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class ToNullablePropertyConverter<TResult, TInput> :
    IPropertyConverter<TResult?, TInput>
    where TResult : struct
{
    readonly IPropertyConverter<TResult, TInput> _converter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="converter">The converter.</param>
    public ToNullablePropertyConverter(IPropertyConverter<TResult, TInput> converter)
    {
        _converter = converter;
    }

    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
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
