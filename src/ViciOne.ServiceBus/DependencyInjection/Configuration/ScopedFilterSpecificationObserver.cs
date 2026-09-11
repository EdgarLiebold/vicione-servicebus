using System;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes scoped filter specification events.</summary>
public class ScopedFilterSpecificationObserver :
    ISendPipeSpecificationObserver,
    IPublishPipeSpecificationObserver
{
    readonly Type _filterType;
    readonly CompositeFilter<Type> _messageTypeFilter;
    readonly IServiceProvider _provider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filterType">The runtime filter type used by the operation.</param>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <param name="messageTypeFilter">The message type filter.</param>
    public ScopedFilterSpecificationObserver(Type filterType, IServiceProvider provider, CompositeFilter<Type> messageTypeFilter)
    {
        _filterType = filterType;
        _provider = provider;
        _messageTypeFilter = messageTypeFilter;
        _messageTypeFilter.Excludes.Add(type => type.ImplementsInterface<Fault>());
        _messageTypeFilter.Excludes.Add(type => type.ImplementsInterface<ReceiveFault>());
        // Serialized scheduler and outbox envelopes bypass application message filters.
        _messageTypeFilter.Excludes.Add(type => type == typeof(SerializedTransportMessage));
    }

    /// <summary>Reports that message specification has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">The specification.</param>
    public void MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
        where T : class
    {
        AddScopedFilter<PublishContext<T>, T>(specification);
    }

    /// <summary>Reports that message specification has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">The specification.</param>
    public void MessageSpecificationCreated<T>(IMessageSendPipeSpecification<T> specification)
        where T : class
    {
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
