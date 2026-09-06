using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Converts a <see cref="Task{TResult}" /> to {T} by awaiting the result.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
public class TaskPropertyConverter<TResult> :
    IPropertyConverter<TResult, Task<TResult?>>,
    IPropertyConverter<Task<TResult?>, TResult>
{
    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
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


/// <summary>Converts a <see cref="Task{T}" /> to {T} by awaiting the result.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class TaskPropertyConverter<TResult, TInput> :
    IPropertyConverter<TResult, Task<TInput?>>,
    IPropertyConverter<Task<TResult?>, TInput>
{
    readonly IPropertyConverter<TResult, TInput> _converter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="converter">The converter.</param>
    public TaskPropertyConverter(IPropertyConverter<TResult, TInput> converter)
    {
        _converter = converter;
    }

    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
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
