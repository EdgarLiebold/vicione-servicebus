using System;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Attaches dependency-injection-scoped filters to message and activity pipelines.</summary>
public static class DependencyInjectionFilterExtensions
{
    /// <summary>Attaches an open-generic scoped filter to matching consume contexts.</summary>
    /// <param name="configurator">The consume pipeline.</param>
    /// <param name="filterType">The open-generic filter implementation.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    public static void UseConsumeFilter(this IConsumePipeConfigurator configurator, Type filterType, IRegistrationContext context)
    {
        UseConsumeFilter(configurator, filterType, context, null);
    }

    /// <summary>Attaches an open-generic scoped filter to selected consume contexts.</summary>
    /// <param name="configurator">The consume pipeline.</param>
    /// <param name="filterType">The open-generic filter implementation.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    /// <param name="configureMessageTypeFilter">An optional callback that selects message contracts.</param>
    public static void UseConsumeFilter(this IConsumePipeConfigurator configurator, Type filterType, IRegistrationContext context,
        Action<IMessageTypeFilterConfigurator>? configureMessageTypeFilter)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        EnsureOpenGenericFilter(filterType, typeof(ConsumeContext<>));

        var messageTypeFilterConfigurator = new MessageTypeFilterConfigurator();
        configureMessageTypeFilter?.Invoke(messageTypeFilterConfigurator);

        var observer = new ScopedConsumePipeSpecificationObserver(filterType, context, messageTypeFilterConfigurator.Filter);

        configurator.ConnectConsumerConfigurationObserver(observer);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>Attaches a closed scoped filter to each compatible consume context.</summary>
    /// <typeparam name="TFilter">The closed filter implementation.</typeparam>
    /// <param name="configurator">The consume pipeline.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    public static void UseConsumeFilter<TFilter>(this IConsumePipeConfigurator configurator, IRegistrationContext context)
        where TFilter : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var filterType = typeof(TFilter);
        EnsureClosedFilter(filterType, typeof(ConsumeContext<>));

        var messageTypeFilterConfigurator = new MessageTypeFilterConfigurator();
        messageTypeFilterConfigurator.Include(type =>
            typeof(IFilter<>).MakeGenericType(typeof(ConsumeContext<>).MakeGenericType(type)).IsAssignableFrom(filterType));

        var observer = new ScopedConsumePipeSpecificationObserver(filterType, context, messageTypeFilterConfigurator.Filter);

        configurator.ConnectConsumerConfigurationObserver(observer);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>Attaches an open-generic scoped filter to matching send contexts.</summary>
    /// <param name="configurator">The send pipeline.</param>
    /// <param name="filterType">The open-generic filter implementation.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    public static void UseSendFilter(this ISendPipelineConfigurator configurator, Type filterType, IRegistrationContext context)
    {
        UseSendFilter(configurator, filterType, context, null);
    }

    /// <summary>Attaches an open-generic scoped filter to selected send contexts.</summary>
    /// <param name="configurator">The send pipeline.</param>
    /// <param name="filterType">The open-generic filter implementation.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    /// <param name="configureMessageTypeFilter">An optional callback that selects message contracts.</param>
    public static void UseSendFilter(this ISendPipelineConfigurator configurator, Type filterType, IRegistrationContext context,
        Action<IMessageTypeFilterConfigurator>? configureMessageTypeFilter)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        EnsureOpenGenericFilter(filterType, typeof(SendContext<>));

        var messageTypeFilterConfigurator = new MessageTypeFilterConfigurator();
        configureMessageTypeFilter?.Invoke(messageTypeFilterConfigurator);

        var observer = new ScopedFilterSpecificationObserver(filterType, context, messageTypeFilterConfigurator.Filter);
        configurator.ConfigureSend(cfg => cfg.ConnectSendPipeSpecificationObserver(observer));
    }

    /// <summary>Attaches a closed scoped filter to each compatible send context.</summary>
    /// <typeparam name="TFilter">The closed filter implementation.</typeparam>
    /// <param name="configurator">The send pipeline.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    public static void UseSendFilter<TFilter>(this ISendPipelineConfigurator configurator, IRegistrationContext context)
        where TFilter : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var filterType = typeof(TFilter);
        EnsureClosedFilter(filterType, typeof(SendContext<>));
        var messageTypeFilterConfigurator = new MessageTypeFilterConfigurator();

        messageTypeFilterConfigurator.Include(type =>
            typeof(IFilter<>).MakeGenericType(typeof(SendContext<>).MakeGenericType(type)).IsAssignableFrom(filterType));

        var observer = new ScopedFilterSpecificationObserver(filterType, context, messageTypeFilterConfigurator.Filter);
        configurator.ConfigureSend(cfg => cfg.ConnectSendPipeSpecificationObserver(observer));
    }

    /// <summary>Attaches an open-generic scoped filter to matching publish contexts.</summary>
    /// <param name="configurator">The publish pipeline.</param>
    /// <param name="filterType">The open-generic filter implementation.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    public static void UsePublishFilter(this IPublishPipelineConfigurator configurator, Type filterType, IRegistrationContext context)
    {
        UsePublishFilter(configurator, filterType, context, null);
    }

    /// <summary>Attaches an open-generic scoped filter to selected publish contexts.</summary>
    /// <param name="configurator">The publish pipeline.</param>
    /// <param name="filterType">The open-generic filter implementation.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    /// <param name="configureMessageTypeFilter">An optional callback that selects message contracts.</param>
    public static void UsePublishFilter(this IPublishPipelineConfigurator configurator, Type filterType, IRegistrationContext context,
        Action<IMessageTypeFilterConfigurator>? configureMessageTypeFilter)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        EnsureOpenGenericFilter(filterType, typeof(PublishContext<>));

        var messageTypeFilterConfigurator = new MessageTypeFilterConfigurator();
        configureMessageTypeFilter?.Invoke(messageTypeFilterConfigurator);

        var observer = new ScopedFilterSpecificationObserver(filterType, context, messageTypeFilterConfigurator.Filter);
        configurator.ConfigurePublish(cfg => cfg.ConnectPublishPipeSpecificationObserver(observer));
    }

    /// <summary>Attaches a closed scoped filter to each compatible publish context.</summary>
    /// <typeparam name="TFilter">The closed filter implementation.</typeparam>
    /// <param name="configurator">The publish pipeline.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    public static void UsePublishFilter<TFilter>(this IPublishPipelineConfigurator configurator, IRegistrationContext context)
        where TFilter : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var filterType = typeof(TFilter);
        EnsureClosedFilter(filterType, typeof(PublishContext<>));
        var messageTypeFilterConfigurator = new MessageTypeFilterConfigurator();

        messageTypeFilterConfigurator.Include(type =>
            typeof(IFilter<>).MakeGenericType(typeof(PublishContext<>).MakeGenericType(type)).IsAssignableFrom(filterType));

        var observer = new ScopedFilterSpecificationObserver(filterType, context, messageTypeFilterConfigurator.Filter);
        configurator.ConfigurePublish(cfg => cfg.ConnectPublishPipeSpecificationObserver(observer));
    }

    /// <summary>Attaches an open-generic scoped filter to matching activity-execution contexts.</summary>
    /// <param name="configurator">The consume pipeline that owns activity execution.</param>
    /// <param name="filterType">The open-generic filter implementation.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    public static void UseExecuteActivityFilter(this IConsumePipeConfigurator configurator, Type filterType, IRegistrationContext context)
    {
        UseExecuteActivityFilter(configurator, filterType, context, null);
    }

    /// <summary>Attaches an open-generic scoped filter to selected activity-execution contexts.</summary>
    /// <param name="configurator">The consume pipeline that owns activity execution.</param>
    /// <param name="filterType">The open-generic filter implementation.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    /// <param name="configureMessageTypeFilter">An optional callback that selects argument contracts.</param>
    public static void UseExecuteActivityFilter(this IConsumePipeConfigurator configurator, Type filterType, IRegistrationContext context,
        Action<IMessageTypeFilterConfigurator>? configureMessageTypeFilter)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        EnsureOpenGenericFilter(filterType, typeof(ExecuteContext<>));

        var messageTypeFilterConfigurator = new MessageTypeFilterConfigurator();
        configureMessageTypeFilter?.Invoke(messageTypeFilterConfigurator);

        var observer = new ScopedExecuteActivityPipeSpecificationObserver(filterType, context, messageTypeFilterConfigurator.Filter);
        configurator.ConnectActivityConfigurationObserver(observer);
    }

    /// <summary>Attaches a closed scoped filter to each compatible activity-execution context.</summary>
    /// <typeparam name="TFilter">The closed filter implementation.</typeparam>
    /// <param name="configurator">The consume pipeline that owns activity execution.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    public static void UseExecuteActivityFilter<TFilter>(this IConsumePipeConfigurator configurator, IRegistrationContext context)
        where TFilter : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var filterType = typeof(TFilter);
        EnsureClosedFilter(filterType, typeof(ExecuteContext<>));
        var messageTypeFilterConfigurator = new MessageTypeFilterConfigurator();

        messageTypeFilterConfigurator.Include(type =>
            typeof(IFilter<>).MakeGenericType(typeof(ExecuteContext<>).MakeGenericType(type)).IsAssignableFrom(filterType));

        var observer = new ScopedExecuteActivityPipeSpecificationObserver(filterType, context, messageTypeFilterConfigurator.Filter);
        configurator.ConnectActivityConfigurationObserver(observer);
    }

    /// <summary>Attaches an open-generic scoped filter to matching activity-compensation contexts.</summary>
    /// <param name="configurator">The consume pipeline that owns activity compensation.</param>
    /// <param name="filterType">The open-generic filter implementation.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    public static void UseCompensateActivityFilter(this IConsumePipeConfigurator configurator, Type filterType, IRegistrationContext context)
    {
        UseCompensateActivityFilter(configurator, filterType, context, null);
    }

    /// <summary>Attaches an open-generic scoped filter to selected activity-compensation contexts.</summary>
    /// <param name="configurator">The consume pipeline that owns activity compensation.</param>
    /// <param name="filterType">The open-generic filter implementation.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    /// <param name="configureMessageTypeFilter">An optional callback that selects log contracts.</param>
    public static void UseCompensateActivityFilter(this IConsumePipeConfigurator configurator, Type filterType, IRegistrationContext context,
        Action<IMessageTypeFilterConfigurator>? configureMessageTypeFilter)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        EnsureOpenGenericFilter(filterType, typeof(CompensateContext<>));

        var messageTypeFilterConfigurator = new MessageTypeFilterConfigurator();
        configureMessageTypeFilter?.Invoke(messageTypeFilterConfigurator);

        var observer = new ScopedCompensateActivityPipeSpecificationObserver(filterType, context, messageTypeFilterConfigurator.Filter);
        configurator.ConnectActivityConfigurationObserver(observer);
    }

    /// <summary>Attaches a closed scoped filter to each compatible activity-compensation context.</summary>
    /// <typeparam name="TFilter">The closed filter implementation.</typeparam>
    /// <param name="configurator">The consume pipeline that owns activity compensation.</param>
    /// <param name="context">The registration context used to resolve filter scopes.</param>
    public static void UseCompensateActivityFilter<TFilter>(this IConsumePipeConfigurator configurator, IRegistrationContext context)
        where TFilter : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var filterType = typeof(TFilter);
        EnsureClosedFilter(filterType, typeof(CompensateContext<>));
        var messageTypeFilterConfigurator = new MessageTypeFilterConfigurator();

        messageTypeFilterConfigurator.Include(type =>
            typeof(IFilter<>).MakeGenericType(typeof(CompensateContext<>).MakeGenericType(type)).IsAssignableFrom(filterType));

        var observer = new ScopedCompensateActivityPipeSpecificationObserver(filterType, context, messageTypeFilterConfigurator.Filter);
        configurator.ConnectActivityConfigurationObserver(observer);
    }

    static void EnsureOpenGenericFilter(Type filterType, Type contextTypeDefinition)
    {
        ArgumentNullException.ThrowIfNull(filterType);

        Type[] parameters = filterType.IsGenericTypeDefinition ? filterType.GetGenericArguments() : Array.Empty<Type>();
        bool implementsExpectedFilter = filterType.IsClass
            && !filterType.IsAbstract
            && parameters.Length == 1
            && ImplementsContextFilter(filterType, contextTypeDefinition, parameters[0]);

        if (!implementsExpectedFilter)
            throw new ArgumentException($"{TypeCache.GetShortName(filterType)} is not an open-generic filter for {TypeCache.GetShortName(contextTypeDefinition)}", nameof(filterType));
    }

    static void EnsureClosedFilter(Type filterType, Type contextTypeDefinition)
    {
        bool implementsExpectedFilter = filterType.IsClass
            && !filterType.IsAbstract
            && !filterType.ContainsGenericParameters
            && ImplementsContextFilter(filterType, contextTypeDefinition, null);

        if (!implementsExpectedFilter)
            throw new ArgumentException($"{TypeCache.GetShortName(filterType)} is not a closed filter for {TypeCache.GetShortName(contextTypeDefinition)}", "TFilter");
    }

    static bool ImplementsContextFilter(Type filterType, Type contextTypeDefinition, Type? expectedContextArgument)
    {
        return filterType.GetInterfaces().Any(interfaceType =>
        {
            if (!interfaceType.IsGenericType || interfaceType.GetGenericTypeDefinition() != typeof(IFilter<>))
                return false;

            Type contextType = interfaceType.GetGenericArguments()[0];
            if (!contextType.IsGenericType || contextType.GetGenericTypeDefinition() != contextTypeDefinition)
                return false;

            return expectedContextArgument == null || contextType.GetGenericArguments()[0] == expectedContextArgument;
        });
    }
}
