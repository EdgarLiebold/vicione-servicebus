using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Combines delegate-based execution and compensation factories for one activity contract.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public sealed class FactoryMethodActivityFactory<TActivity, TArguments, TLog> :
    IActivityFactory<TActivity, TArguments, TLog>
    where TActivity : class, IExecuteActivity<TArguments>, ICompensateActivity<TLog>
    where TArguments : class
    where TLog : class
{
    readonly ICompensateActivityFactory<TActivity, TLog> _compensateFactory;
    readonly IExecuteActivityFactory<TActivity, TArguments> _executeFactory;

    /// <summary>Initializes the paired activity factories.</summary>
    /// <param name="executeFactory">The delegate that creates an activity from its arguments.</param>
    /// <param name="compensateFactory">The delegate that creates an activity from its compensation log.</param>
    public FactoryMethodActivityFactory(Func<TArguments, TActivity> executeFactory,
        Func<TLog, TActivity> compensateFactory)
    {
        ArgumentNullException.ThrowIfNull(executeFactory);
        ArgumentNullException.ThrowIfNull(compensateFactory);

        _executeFactory = new FactoryMethodExecuteActivityFactory<TActivity, TArguments>(executeFactory);
        _compensateFactory = new FactoryMethodCompensateActivityFactory<TActivity, TLog>(compensateFactory);
    }

    /// <summary>Creates an activity for the supplied arguments and invokes the execution pipeline.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(ExecuteContext<TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next, CancellationToken cancellationToken = default)
    {
        return _executeFactory.ExecuteAsync(context, next, cancellationToken: cancellationToken);
    }

    /// <summary>Creates an activity for the supplied log and invokes the compensation pipeline.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CompensateAsync(CompensateContext<TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default)
    {
        return _compensateFactory.CompensateAsync(context, next, cancellationToken: cancellationToken);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("factoryMethod");
    }
}
