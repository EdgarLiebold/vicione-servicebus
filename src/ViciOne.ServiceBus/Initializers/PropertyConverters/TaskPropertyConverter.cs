using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Wraps a property value in a task or awaits a task-valued property.</summary>
/// <typeparam name="TResult">The task result type.</typeparam>
internal sealed class TaskPropertyConverter<TResult> :
    IPropertyConverter<TResult, Task<TResult?>>,
    IPropertyConverter<Task<TResult?>, TResult>
{
    /// <inheritdoc />
    public Task<Task<TResult?>?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TResult? input,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<Task<TResult?>?>(cancellationToken);

        return Task.FromResult<Task<TResult?>?>(Task.FromResult(input));
    }

    async Task<TResult?> IPropertyConverter<TResult, Task<TResult?>>.ConvertAsync<T>(InitializeContext<T> context, Task<TResult?>? input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (input == null)
            return default;

        return await input.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}


/// <summary>Combines task wrapping or awaiting with a conversion of the underlying value.</summary>
/// <typeparam name="TResult">The converted task result type.</typeparam>
/// <typeparam name="TInput">The source task result type.</typeparam>
internal sealed class TaskPropertyConverter<TResult, TInput> :
    IPropertyConverter<TResult, Task<TInput?>>,
    IPropertyConverter<Task<TResult?>, TInput>
{
    readonly IPropertyConverter<TResult, TInput> _converter;

    /// <summary>Creates a task adapter around <paramref name="converter" />.</summary>
    /// <param name="converter">The underlying value conversion.</param>
    public TaskPropertyConverter(IPropertyConverter<TResult, TInput> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    /// <inheritdoc />
    public Task<Task<TResult?>?> ConvertAsync<T>(InitializeContext<T> context, TInput? input, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<Task<TResult?>?>(cancellationToken);

        Task<TResult?> conversionTask = _converter.ConvertAsync(context, input, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The property converter returned a null task.");
        return Task.FromResult<Task<TResult?>?>(conversionTask);
    }

    Task<TResult?> IPropertyConverter<TResult, Task<TInput?>>.ConvertAsync<T>(InitializeContext<T> context, Task<TInput?>? input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<TResult?>(cancellationToken);

        if (input == default)
            return TaskResults.DefaultAsync<TResult>();

        if (input.IsCompletedSuccessfully)
            return _converter.ConvertAsync(context, input.GetAwaiter().GetResult(), cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("The property converter returned a null task.");

        async Task<TResult?> ConvertAsync()
        {
            var value = await input.WaitAsync(cancellationToken).ConfigureAwait(false);

            Task<TResult?> convertTask = _converter.ConvertAsync(context, value, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("The property converter returned a null task.");
            if (convertTask.IsCompletedSuccessfully)
                return convertTask.GetAwaiter().GetResult();

            return await convertTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return ConvertAsync();
    }
}
