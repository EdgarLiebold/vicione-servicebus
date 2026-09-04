using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>
/// Provides an initialize property converter implementation.
/// </summary>
/// <typeparam name="TProperty">The t property type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
public class InitializePropertyConverter<TProperty, TInput> :
    IPropertyConverter<TProperty, TInput>
    where TProperty : class
    where TInput : class
{
    readonly IMessageInitializer<TProperty> _initializer;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public InitializePropertyConverter()
    {
        _initializer = MessageInitializerCache<TProperty>.GetInitializer(typeof(TInput));
    }

    Task<TProperty?> IPropertyConverter<TProperty, TInput>.ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input, CancellationToken cancellationToken)
    {
        if (input == null)
            return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

        InitializeContext<TProperty> messageContext = MessageFactoryCache<TProperty>.Factory.Create(context);

        Task<InitializeContext<TProperty>> initTask = _initializer.InitializeAsync(messageContext, input, cancellationToken: cancellationToken);
        if (initTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<TProperty?>(initTask.Result.Message);

        async Task<TProperty?> ConvertAsync()
        {
            InitializeContext<TProperty> result = await initTask.ConfigureAwait(false);

            return result.Message;
        }

        return ConvertAsync();
    }
}


/// <summary>
/// Provides an initialize property converter implementation.
/// </summary>
/// <typeparam name="TProperty">The t property type.</typeparam>
public class InitializePropertyConverter<TProperty> :
    IPropertyConverter<TProperty, object>
    where TProperty : class
{
    Task<TProperty?> IPropertyConverter<TProperty, object>.ConvertAsync<TMessage>(InitializeContext<TMessage> context, object? input, CancellationToken cancellationToken)
    {
        if (input == null)
            return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

        InitializeContext<TProperty> messageContext = MessageFactoryCache<TProperty>.Factory.Create(context);

        IMessageInitializer<TProperty> initializer = MessageInitializerCache<TProperty>.GetInitializer(input.GetType());

        Task<InitializeContext<TProperty>> initTask = initializer.InitializeAsync(messageContext, input, cancellationToken: cancellationToken);
        if (initTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<TProperty?>(initTask.Result.Message);

        async Task<TProperty?> ConvertAsync()
        {
            InitializeContext<TProperty> result = await initTask.ConfigureAwait(false);

            return result.Message;
        }

        return ConvertAsync();
    }
}
