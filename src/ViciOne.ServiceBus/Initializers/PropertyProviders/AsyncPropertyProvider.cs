using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Resolves a task-valued property and awaits its caller-owned value.</summary>
/// <remarks>
/// The outer provider operation is observed to its original completion. Waiting on the supplied
/// task value remains locally cancellable without completing that caller-owned task.
/// Missing input or a missing task value produces the default property value.
/// </remarks>
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

    /// <summary>Observes outer resolution, then waits locally for the supplied task value.</summary>
    /// <typeparam name="T">The message contract being initialized.</typeparam>
    /// <param name="context">The message and input object supplied to the provider.</param>
    /// <param name="cancellationToken">The cooperative provider token and local input-value wait token.</param>
    /// <returns>A task containing the input task's original result, or the default value when absent.</returns>
    async Task<TProperty?> IPropertyProvider<TInput, TProperty>.GetPropertyAsync<T>(InitializeContext<T, TInput> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.HasInput)
            return default;

        Task<Task<TProperty?>?> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The task-property provider returned null.");
        Task<TProperty?>? valueTask = await propertyTask.ConfigureAwait(false);
        if (valueTask == null)
            return default;

        return await valueTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}


/// <summary>Resolves a task-valued property, awaits its caller-owned value and converts the result.</summary>
/// <remarks>
/// Started outer-provider and converter operations remain observed to their original completion.
/// Only the supplied task-value wait is locally cancellable. Successful accepted resolution
/// forwards the original caller token to conversion without adding an inter-stage cancellation gate.
/// Missing input or a missing task value produces the default value without conversion.
/// </remarks>
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

    /// <summary>Observes provider resolution, locally awaits the task value and observes conversion.</summary>
    /// <typeparam name="T">The message contract being initialized.</typeparam>
    /// <param name="context">The message and input object supplied to provider and converter.</param>
    /// <param name="cancellationToken">The cooperative collaborator token and local input-value wait token.</param>
    /// <returns>A task containing the original conversion result, or the default value when input is absent.</returns>
    async Task<TProperty?> IPropertyProvider<TInput, TProperty>.GetPropertyAsync<T>(InitializeContext<T, TInput> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.HasInput)
            return default;

        Task<Task<TTask?>?> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The task-property provider returned null.");
        Task<TTask?>? valueTask = await propertyTask.ConfigureAwait(false);
        if (valueTask == null)
            return default;

        var value = await valueTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        Task<TProperty?> conversionTask = _converter.ConvertAsync(context, value, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The property converter returned null.");
        return await conversionTask.ConfigureAwait(false);
    }
}
