using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Creates factory method activity instances.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public class FactoryMethodActivityFactory<TActivity, TArguments, TLog> :
    IActivityFactory<TActivity, TArguments, TLog>
    where TActivity : class, IExecuteActivity<TArguments>, ICompensateActivity<TLog>
    where TArguments : class
    where TLog : class
{
    readonly ICompensateActivityFactory<TActivity, TLog> _compensateFactory;
    readonly IExecuteActivityFactory<TActivity, TArguments> _executeFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="executeFactory">The execute factory.</param>
    /// <param name="compensateFactory">The compensate factory.</param>
    public FactoryMethodActivityFactory(Func<TArguments, TActivity> executeFactory,
        Func<TLog, TActivity> compensateFactory)
    {
        _executeFactory = new FactoryMethodExecuteActivityFactory<TActivity, TArguments>(executeFactory);
        _compensateFactory = new FactoryMethodCompensateActivityFactory<TActivity, TLog>(compensateFactory);
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(ExecuteContext<TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next, CancellationToken cancellationToken = default)
    {
        return _executeFactory.ExecuteAsync(context, next, cancellationToken: cancellationToken);
    }

    /// <summary>Compensates the completed activity.</summary>
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
        context.CreateScope("factoryMethod");
    }
}
