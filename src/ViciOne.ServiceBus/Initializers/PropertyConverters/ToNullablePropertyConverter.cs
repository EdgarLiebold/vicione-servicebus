using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>
/// Provides a to nullable property converter implementation.
/// </summary>
/// <typeparam name="TResult">The t result type.</typeparam>
public class ToNullablePropertyConverter<TResult> :
    IPropertyConverter<TResult?, TResult>
    where TResult : struct
{
    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="input">The input value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TResult?> ConvertAsync<T>(InitializeContext<T> context, TResult input, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<TResult?>(cancellationToken); return Task.FromResult<TResult?>(input);
    }
}


/// <summary>
/// Provides a to nullable property converter implementation.
/// </summary>
/// <typeparam name="TResult">The t result type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
public class ToNullablePropertyConverter<TResult, TInput> :
    IPropertyConverter<TResult?, TInput>
    where TResult : struct
{
    readonly IPropertyConverter<TResult, TInput> _converter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="converter">The converter value.</param>
    public ToNullablePropertyConverter(IPropertyConverter<TResult, TInput> converter)
    {
        _converter = converter;
    }

    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="input">The input value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
