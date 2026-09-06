using System;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes scoped compensate activity pipe specification events.</summary>
public class ScopedCompensateActivityPipeSpecificationObserver :
    IActivityConfigurationObserver
{
    readonly IRegistrationContext _context;
    readonly Type _filterType;
    readonly CompositeFilter<Type> _messageTypeFilter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filterType">The runtime filter type used by the operation.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="messageTypeFilter">The message type filter.</param>
    public ScopedCompensateActivityPipeSpecificationObserver(Type filterType, IRegistrationContext context,
        CompositeFilter<Type> messageTypeFilter)
    {
        _filterType = filterType;
        _context = context;
        _messageTypeFilter = messageTypeFilter;
    }

    /// <summary>Reports that activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
        Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
    }

    /// <summary>Reports that execute activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
    }

    /// <summary>Reports that compensate activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
    {
        if (!_messageTypeFilter.Matches(typeof(TLog)))
            return;

        var filterType = _filterType.MakeGenericType(typeof(TLog));

        if (!filterType.ImplementsInterface(typeof(IFilter<CompensateContext<TLog>>)))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Scoped Compensate Activity Pipe Specification Observer", "unknown", $"The scoped filter must implement {TypeCache<IFilter<CompensateContext<TLog>>>.ShortName} ", "Correct the named configuration before starting the host"));

        var scopeProvider = new CompensateScopeProvider<TLog>(_context);

        var scopedFilterType = typeof(ScopedCompensateFilter<,>).MakeGenericType(typeof(TLog), filterType);

        var filter = (IFilter<CompensateContext<TLog>>)(Activator.CreateInstance(scopedFilterType, scopeProvider) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        var specification = new FilterPipeSpecification<CompensateContext<TLog>>(filter);

        configurator.Log(x => x.AddPipeSpecification(specification));
    }
}
