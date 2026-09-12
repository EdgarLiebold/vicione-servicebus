using System.Threading.Tasks;
namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Awaits a task-valued property with caller cancellation.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
internal sealed class AsyncPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly IPropertyProvider<TInput, Task<TProperty?>> _provider;

    /// <summary>Creates a provider that unwraps the task returned by <paramref name="provider"/>.</summary>
    /// <param name="provider">The provider that supplies the task-valued property.</param>
    public AsyncPropertyProvider(IPropertyProvider<TInput, Task<TProperty?>> provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    async Task<TProperty?> IPropertyProvider<TInput, TProperty>.GetPropertyAsync<T>(InitializeContext<T, TInput> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.HasInput)
            return default;

        Task<Task<TProperty?>?> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The task-property provider returned null.");
        Task<TProperty?>? valueTask = await propertyTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (valueTask == null)
            return default;

        return await valueTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}


/// <summary>Awaits a task-valued property and converts its result with caller cancellation.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <typeparam name="TTask">The task result type.</typeparam>
internal sealed class AsyncPropertyProvider<TInput, TProperty, TTask> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly IPropertyConverter<TProperty, TTask> _converter;
    readonly IPropertyProvider<TInput, Task<TTask?>> _provider;

    /// <summary>Creates a provider that unwraps and converts a task-valued property.</summary>
    /// <param name="provider">The provider that supplies the task-valued property.</param>
    /// <param name="converter">The converter applied to the awaited value.</param>
    public AsyncPropertyProvider(IPropertyProvider<TInput, Task<TTask?>> provider, IPropertyConverter<TProperty, TTask> converter)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    async Task<TProperty?> IPropertyProvider<TInput, TProperty>.GetPropertyAsync<T>(InitializeContext<T, TInput> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.HasInput)
            return default;

        Task<Task<TTask?>?> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The task-property provider returned null.");
        Task<TTask?>? valueTask = await propertyTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (valueTask == null)
            return default;

        var value = await valueTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        Task<TProperty?> conversionTask = _converter.ConvertAsync(context, value, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The property converter returned null.");
        return await conversionTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}
