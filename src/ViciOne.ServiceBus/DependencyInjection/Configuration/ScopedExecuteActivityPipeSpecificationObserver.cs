using System;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a scoped execute activity pipe specification observer implementation.
/// </summary>
public class ScopedExecuteActivityPipeSpecificationObserver :
    IActivityConfigurationObserver
{
    readonly IRegistrationContext _context;
    readonly Type _filterType;
    readonly CompositeFilter<Type> _messageTypeFilter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="filterType">The filter type value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="messageTypeFilter">The message type filter value.</param>
    public ScopedExecuteActivityPipeSpecificationObserver(Type filterType, IRegistrationContext context,
        CompositeFilter<Type> messageTypeFilter)
    {
        _filterType = filterType;
        _context = context;
        _messageTypeFilter = messageTypeFilter;
    }

    /// <summary>
    /// Performs the activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
        Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
        ExecuteActivityConfigured(configurator);
    }

    /// <summary>
    /// Performs the execute activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        if (!_messageTypeFilter.Matches(typeof(TArguments)))
            return;

        var filterType = _filterType.MakeGenericType(typeof(TArguments));

        if (!filterType.ImplementsInterface(typeof(IFilter<ExecuteContext<TArguments>>)))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Scoped Execute Activity Pipe Specification Observer", "unknown", $"The scoped filter must implement {TypeCache<IFilter<ExecuteContext<TArguments>>>.ShortName} ", "Correct the named configuration before starting the host"));

        var scopeProvider = new ExecuteScopeProvider<TArguments>(_context);

        var scopedFilterType = typeof(ScopedExecuteFilter<,>).MakeGenericType(typeof(TArguments), filterType);

        var filter = (IFilter<ExecuteContext<TArguments>>)(Activator.CreateInstance(scopedFilterType, scopeProvider) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        var specification = new FilterPipeSpecification<ExecuteContext<TArguments>>(filter);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>
    /// Performs the compensate activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
    {
    }
}
