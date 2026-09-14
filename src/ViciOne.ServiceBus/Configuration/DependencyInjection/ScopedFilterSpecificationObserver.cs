using System;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Attaches a dependency-injection-scoped filter to selected send or publish message pipelines.</summary>
internal sealed class ScopedFilterSpecificationObserver :
    ISendPipeSpecificationObserver,
    IPublishPipeSpecificationObserver
{
    readonly Type _filterType;
    readonly CompositeFilter<Type> _messageTypeFilter;
    readonly IServiceProvider _provider;

    /// <summary>Creates an observer for one filter implementation and message-selection policy.</summary>
    /// <param name="filterType">The closed filter or open-generic filter definition.</param>
    /// <param name="provider">The service provider used to create filter scopes.</param>
    /// <param name="messageTypeFilter">The policy that selects message contracts.</param>
    public ScopedFilterSpecificationObserver(Type filterType, IServiceProvider provider, CompositeFilter<Type> messageTypeFilter)
    {
        _filterType = filterType ?? throw new ArgumentNullException(nameof(filterType));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _messageTypeFilter = messageTypeFilter ?? throw new ArgumentNullException(nameof(messageTypeFilter));
        _messageTypeFilter.Excludes.Add(type => type.ImplementsInterface<Fault>());
        _messageTypeFilter.Excludes.Add(type => type.ImplementsInterface<ReceiveFault>());
        // Serialized scheduler and outbox envelopes bypass application message filters.
        _messageTypeFilter.Excludes.Add(type => type == typeof(SerializedTransportMessage));
    }

    /// <summary>Attaches the scoped filter to a publish-message specification.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="specification">The publish pipeline specification.</param>
    public void MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(specification);

        AddScopedFilter<PublishContext<T>, T>(specification);
    }

    /// <summary>Attaches the scoped filter to a send-message specification.</summary>
    /// <typeparam name="T">The sent message contract.</typeparam>
    /// <param name="specification">The send pipeline specification.</param>
    public void MessageSpecificationCreated<T>(IMessageSendPipeSpecification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(specification);

        AddScopedFilter<SendContext<T>, T>(specification);
    }

    void AddScopedFilter<TContext, T>(IPipeConfigurator<TContext> configurator)
        where TContext : class, PipeContext
        where T : class
    {
        if (!_messageTypeFilter.Matches(typeof(T)))
            return;

        var filterType = _filterType.ImplementsInterface<IFilter<TContext>>()
            ? _filterType
            : _filterType.MakeGenericType(typeof(T));

        if (!filterType.ImplementsInterface(typeof(IFilter<TContext>)))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Scoped Filter Specification Observer", "unknown", $"The scoped filter must implement {TypeCache<IFilter<TContext>>.ShortName} ", "Correct the named configuration before starting the host"));

        var scopeProviderType = typeof(FilterScopeProvider<,>).MakeGenericType(filterType, typeof(TContext));

        var scopeProvider = (IFilterScopeProvider<TContext>)(Activator.CreateInstance(scopeProviderType, _provider) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        var filter = new ScopedFilter<TContext>(scopeProvider);
        var specification = new FilterPipeSpecification<TContext>(filter);

        configurator.AddPipeSpecification(specification);
    }
}
