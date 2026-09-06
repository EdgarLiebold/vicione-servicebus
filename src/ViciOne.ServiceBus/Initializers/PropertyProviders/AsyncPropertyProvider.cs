using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Awaits a <see cref="Task{TProperty}" /> property, returning the property value.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class AsyncPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly IPropertyProvider<TInput, Task<TProperty?>> _provider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public AsyncPropertyProvider(IPropertyProvider<TInput, Task<TProperty?>> provider)
    {
        _provider = provider;
    }

    Task<TProperty?> IPropertyProvider<TInput, TProperty>.GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken)
    {
        if (!context.HasInput)
            return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

        Task<Task<TProperty?>?> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (propertyTask.Status == TaskStatus.RanToCompletion)
        {
            Task<TProperty?>? valueTask = propertyTask.Result;
            if (valueTask == null)
                return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

            if (valueTask.Status == TaskStatus.RanToCompletion)
                return valueTask;
        }

        async Task<TProperty?> GetPropertyAsync()
        {
            Task<TProperty?>? valueTask = await propertyTask.ConfigureAwait(false);
            if (valueTask == null)
                return default;

            return await valueTask.ConfigureAwait(false);
        }

        return GetPropertyAsync();
    }
}


/// <summary>Awaits a <see cref="Task{TProperty}" /> property, returning the property value.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <typeparam name="TTask">The ask type.</typeparam>
public class AsyncPropertyProvider<TInput, TProperty, TTask> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly IPropertyConverter<TProperty, TTask> _converter;
    readonly IPropertyProvider<TInput, Task<TTask?>> _provider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <param name="converter">The converter.</param>
    public AsyncPropertyProvider(IPropertyProvider<TInput, Task<TTask?>> provider, IPropertyConverter<TProperty, TTask> converter)
    {
        _provider = provider;
        _converter = converter;
    }

    Task<TProperty?> IPropertyProvider<TInput, TProperty>.GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken)
    {
        if (!context.HasInput)
            return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

        Task<Task<TTask?>?> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (propertyTask.Status == TaskStatus.RanToCompletion)
        {
            Task<TTask?>? valueTask = propertyTask.Result;
            if (valueTask == null)
                return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

            if (valueTask.Status == TaskStatus.RanToCompletion)
                return _converter.ConvertAsync(context, valueTask.Result, cancellationToken: cancellationToken);
        }

        async Task<TProperty?> GetPropertyAsync()
        {
            Task<TTask?>? valueTask = await propertyTask.ConfigureAwait(false);
            if (valueTask == null)
                return default;

            var value = await valueTask.ConfigureAwait(false);

            return await _converter.ConvertAsync(context, value, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return GetPropertyAsync();
    }
}
