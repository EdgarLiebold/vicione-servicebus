using System;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Attaches a dependency-injection-scoped filter to selected routing-slip compensation pipelines.</summary>
internal sealed class ScopedCompensateActivityPipeSpecificationObserver :
    IActivityConfigurationObserver
{
    readonly IRegistrationContext _context;
    readonly Type _filterType;
    readonly CompositeFilter<Type> _messageTypeFilter;

    /// <summary>Creates an observer for one filter implementation and log-selection policy.</summary>
    /// <param name="filterType">The closed filter or open-generic filter definition.</param>
    /// <param name="context">The registration context used to create compensation scopes.</param>
    /// <param name="messageTypeFilter">The policy that selects compensation-log contracts.</param>
    public ScopedCompensateActivityPipeSpecificationObserver(Type filterType, IRegistrationContext context,
        CompositeFilter<Type> messageTypeFilter)
    {
        _filterType = filterType ?? throw new ArgumentNullException(nameof(filterType));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _messageTypeFilter = messageTypeFilter ?? throw new ArgumentNullException(nameof(messageTypeFilter));
    }

    /// <summary>Leaves activity execution unchanged because this observer owns compensation filters.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execution-argument contract.</typeparam>
    /// <param name="configurator">The configured execution pipeline.</param>
    /// <param name="compensateAddress">The companion compensation endpoint address.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
        Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
        // Execution and compensation filters have different pipe-context contracts.
    }

    /// <summary>Leaves execute-only activities unchanged because they have no compensation pipeline.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execution-argument contract.</typeparam>
    /// <param name="configurator">The configured execution pipeline.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        // Execute-only activities do not produce a compensation log.
    }

    /// <summary>Attaches the scoped filter to a compensation-log pipeline.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TLog">The compensation-log contract.</typeparam>
    /// <param name="configurator">The configured compensation pipeline.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (!_messageTypeFilter.Matches(typeof(TLog)))
            return;

        var filterType = _filterType.ImplementsInterface<IFilter<CompensateContext<TLog>>>()
            ? _filterType
            : _filterType.MakeGenericType(typeof(TLog));

        if (!filterType.ImplementsInterface(typeof(IFilter<CompensateContext<TLog>>)))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Scoped Compensate Activity Pipe Specification Observer", "unknown", $"The scoped filter must implement {TypeCache<IFilter<CompensateContext<TLog>>>.ShortName} ", "Correct the named configuration before starting the host"));

        var scopeProvider = new CompensateScopeProvider<TLog>(_context);

        var scopedFilterType = typeof(ScopedCompensateFilter<,>).MakeGenericType(typeof(TLog), filterType);

        var filter = (IFilter<CompensateContext<TLog>>)(Activator.CreateInstance(scopedFilterType, scopeProvider) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        var specification = new FilterPipeSpecification<CompensateContext<TLog>>(filter);

        configurator.Log(x => x.AddPipeSpecification(specification));
    }
}
