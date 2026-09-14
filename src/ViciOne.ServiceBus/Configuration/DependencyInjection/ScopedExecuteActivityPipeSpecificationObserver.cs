using System;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Attaches a dependency-injection-scoped filter to selected routing-slip execution pipelines.</summary>
internal sealed class ScopedExecuteActivityPipeSpecificationObserver :
    IActivityConfigurationObserver
{
    readonly IRegistrationContext _context;
    readonly Type _filterType;
    readonly CompositeFilter<Type> _messageTypeFilter;

    /// <summary>Creates an observer for one filter implementation and argument-selection policy.</summary>
    /// <param name="filterType">The closed filter or open-generic filter definition.</param>
    /// <param name="context">The registration context used to create execution scopes.</param>
    /// <param name="messageTypeFilter">The policy that selects activity argument contracts.</param>
    public ScopedExecuteActivityPipeSpecificationObserver(Type filterType, IRegistrationContext context,
        CompositeFilter<Type> messageTypeFilter)
    {
        _filterType = filterType ?? throw new ArgumentNullException(nameof(filterType));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _messageTypeFilter = messageTypeFilter ?? throw new ArgumentNullException(nameof(messageTypeFilter));
    }

    /// <summary>Attaches the execution filter for a compensating activity.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execution-argument contract.</typeparam>
    /// <param name="configurator">The configured execution pipeline.</param>
    /// <param name="compensateAddress">The companion compensation endpoint address.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
        Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
        ExecuteActivityConfigured(configurator);
    }

    /// <summary>Attaches the execution filter for an execute-only activity.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execution-argument contract.</typeparam>
    /// <param name="configurator">The configured execution pipeline.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (!_messageTypeFilter.Matches(typeof(TArguments)))
            return;

        var filterType = _filterType.ImplementsInterface<IFilter<ExecuteContext<TArguments>>>()
            ? _filterType
            : _filterType.MakeGenericType(typeof(TArguments));

        if (!filterType.ImplementsInterface(typeof(IFilter<ExecuteContext<TArguments>>)))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Scoped Execute Activity Pipe Specification Observer", "unknown", $"The scoped filter must implement {TypeCache<IFilter<ExecuteContext<TArguments>>>.ShortName} ", "Correct the named configuration before starting the host"));

        var scopeProvider = new ExecuteScopeProvider<TArguments>(_context);

        var scopedFilterType = typeof(ScopedExecuteFilter<,>).MakeGenericType(typeof(TArguments), filterType);

        var filter = (IFilter<ExecuteContext<TArguments>>)(Activator.CreateInstance(scopedFilterType, scopeProvider) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        var specification = new FilterPipeSpecification<ExecuteContext<TArguments>>(filter);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Leaves compensation pipelines unchanged because this observer owns execution filters.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TLog">The compensation-log contract.</typeparam>
    /// <param name="configurator">The configured compensation pipeline.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
    {
        // Execution and compensation filters have different pipe-context contracts.
    }
}
