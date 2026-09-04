using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>
/// Converts a <see cref="Task{TResult}" /> to {T} by awaiting the result
/// </summary>
/// <typeparam name="TResult"></typeparam>
public class TaskPropertyConverter<TResult> :
    IPropertyConverter<TResult, Task<TResult?>>,
    IPropertyConverter<Task<TResult?>, TResult>
{
    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="input">The input value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Task<TResult?>?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TResult? input,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::System.Threading.Tasks.Task<TResult?>?>(cancellationToken); return Task.FromResult<Task<TResult?>?>(Task.FromResult(input));
    }

    async Task<TResult?> IPropertyConverter<TResult, Task<TResult?>>.ConvertAsync<T>(InitializeContext<T> context, Task<TResult?>? input,
        CancellationToken cancellationToken)
    {
        if (input == null)
            return default;

        return await input.ConfigureAwait(false);
    }
}


/// <summary>
/// Converts a <see cref="Task{T}" /> to {T} by awaiting the result
/// </summary>
/// <typeparam name="TResult"></typeparam>
/// <typeparam name="TInput"></typeparam>
public class TaskPropertyConverter<TResult, TInput> :
    IPropertyConverter<TResult, Task<TInput?>>,
    IPropertyConverter<Task<TResult?>, TInput>
{
    readonly IPropertyConverter<TResult, TInput> _converter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="converter">The converter value.</param>
    public TaskPropertyConverter(IPropertyConverter<TResult, TInput> converter)
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
    public Task<Task<TResult?>?> ConvertAsync<T>(InitializeContext<T> context, TInput? input, CancellationToken cancellationToken = default)
        where T : class
    {
        return Task.FromResult<Task<TResult?>?>(_converter.ConvertAsync(context, input, cancellationToken: cancellationToken));
    }

    Task<TResult?> IPropertyConverter<TResult, Task<TInput?>>.ConvertAsync<T>(InitializeContext<T> context, Task<TInput?>? input,
        CancellationToken cancellationToken)
    {
        if (input == default)
            return TaskResults.DefaultAsync<TResult>(cancellationToken: cancellationToken);

        if (input.Status == TaskStatus.RanToCompletion)
            return _converter.ConvertAsync(context, input.Result, cancellationToken: cancellationToken);

        async Task<TResult?> ConvertAsync()
        {
            var value = await input.ConfigureAwait(false);

            Task<TResult?> convertTask = _converter.ConvertAsync(context, value, cancellationToken: cancellationToken);
            if (convertTask.Status == TaskStatus.RanToCompletion)
                return convertTask.Result;

            return await convertTask.ConfigureAwait(false);
        }

        return ConvertAsync();
    }
}
