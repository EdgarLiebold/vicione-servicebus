using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Initializes a nested message property from a typed input object.</summary>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
internal sealed class InitializePropertyConverter<TProperty, TInput> :
    IPropertyConverter<TProperty, TInput>
    where TProperty : class
    where TInput : class
{
    readonly IMessageInitializer<TProperty> _initializer;

    /// <summary>Resolves and caches the nested initializer for the declared input type.</summary>
    public InitializePropertyConverter()
    {
        _initializer = MessageInitializerCache<TProperty>.GetInitializer(typeof(TInput));
    }

    async Task<TProperty?> IPropertyConverter<TProperty, TInput>.ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (input == null)
            return default;

        InitializeContext<TProperty> messageContext = MessageFactoryCache<TProperty>.Factory.Create(context);

        Task<InitializeContext<TProperty>> initTask = _initializer.InitializeAsync(messageContext, input, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The nested message initializer returned null.");
        InitializeContext<TProperty> result = await initTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        return result.Message;
    }
}


/// <summary>Initializes a nested message property from a runtime input object.</summary>
/// <typeparam name="TProperty">The property type.</typeparam>
internal sealed class InitializePropertyConverter<TProperty> :
    IPropertyConverter<TProperty, object>
    where TProperty : class
{
    async Task<TProperty?> IPropertyConverter<TProperty, object>.ConvertAsync<TMessage>(InitializeContext<TMessage> context, object? input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (input == null)
            return default;

        InitializeContext<TProperty> messageContext = MessageFactoryCache<TProperty>.Factory.Create(context);

        IMessageInitializer<TProperty> initializer = MessageInitializerCache<TProperty>.GetInitializer(input.GetType());

        Task<InitializeContext<TProperty>> initTask = initializer.InitializeAsync(messageContext, input, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The nested message initializer returned null.");
        InitializeContext<TProperty> result = await initTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        return result.Message;
    }
}
