using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

/// <summary>Initializes a nested message property from a typed input object.</summary>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
/// <remarks>An accepted nested initialization is observed to its original terminal outcome.</remarks>
internal sealed class InitializePropertyConverter<TProperty, TInput> :
    IPropertyConverter<TProperty, TInput>
    where TProperty : class
    where TInput : class
{
    readonly IMessageInitializer<TProperty> _initializer;

    /// <summary>Resolves and caches the nested initializer for the declared input type.</summary>
    public InitializePropertyConverter()
        : this(MessageInitializerCache<TProperty>.GetInitializer(typeof(TInput)))
    {
    }

    /// <summary>Creates a nested converter with an explicitly supplied initializer.</summary>
    /// <param name="initializer">The initializer that populates the nested message.</param>
    public InitializePropertyConverter(IMessageInitializer<TProperty> initializer)
    {
        _initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
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
        InitializeContext<TProperty> result = await initTask.ConfigureAwait(false);
        return result.Message;
    }
}


/// <summary>Initializes a nested message property from a runtime input object.</summary>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <remarks>An accepted nested initialization is observed to its original terminal outcome.</remarks>
internal sealed class InitializePropertyConverter<TProperty> :
    IPropertyConverter<TProperty, object>
    where TProperty : class
{
    readonly Func<Type, IMessageInitializer<TProperty>> _initializerResolver;

    /// <summary>Creates a converter that resolves nested initializers from the shared cache.</summary>
    public InitializePropertyConverter()
        : this(MessageInitializerCache<TProperty>.GetInitializer)
    {
    }

    /// <summary>Creates a converter with an explicit runtime-type initializer resolver.</summary>
    /// <param name="initializerResolver">The resolver used for the runtime input type.</param>
    public InitializePropertyConverter(Func<Type, IMessageInitializer<TProperty>> initializerResolver)
    {
        _initializerResolver = initializerResolver ?? throw new ArgumentNullException(nameof(initializerResolver));
    }

    async Task<TProperty?> IPropertyConverter<TProperty, object>.ConvertAsync<TMessage>(InitializeContext<TMessage> context, object? input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (input == null)
            return default;

        InitializeContext<TProperty> messageContext = MessageFactoryCache<TProperty>.Factory.Create(context);

        IMessageInitializer<TProperty> initializer = _initializerResolver(input.GetType())
            ?? throw new InvalidOperationException("The nested message initializer resolver returned null.");

        Task<InitializeContext<TProperty>> initTask = initializer.InitializeAsync(messageContext, input, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The nested message initializer returned null.");
        InitializeContext<TProperty> result = await initTask.ConfigureAwait(false);
        return result.Message;
    }
}
