using System;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a scoped compensate activity pipe specification observer implementation.
/// </summary>
public class ScopedCompensateActivityPipeSpecificationObserver :
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
    public ScopedCompensateActivityPipeSpecificationObserver(Type filterType, IRegistrationContext context,
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
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
    }

    /// <summary>
    /// Performs the execute activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
    }

    /// <summary>
    /// Performs the compensate activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityConfigurator<TActivity, TLog> configurator)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        if (!_messageTypeFilter.Matches(typeof(TLog)))
            return;

        var filterType = _filterType.MakeGenericType(typeof(TLog));

        if (!filterType.ImplementsInterface(typeof(IFilter<CompensateContext<TLog>>)))
            throw new ConfigurationException($"The scoped filter must implement {TypeCache<IFilter<CompensateContext<TLog>>>.ShortName} ");

        var scopeProvider = new CompensateActivityScopeProvider<TActivity, TLog>(_context);

        var scopedFilterType = typeof(ScopedCompensateFilter<,,>).MakeGenericType(typeof(TActivity), typeof(TLog), filterType);

        var filter = (IFilter<CompensateContext<TLog>>)(Activator.CreateInstance(scopedFilterType, scopeProvider) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        var specification = new FilterPipeSpecification<CompensateContext<TLog>>(filter);

        configurator.Log(x => x.AddPipeSpecification(specification));
    }
}
