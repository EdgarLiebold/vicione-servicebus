using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Provides a factory method activity factory implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public class FactoryMethodActivityFactory<TActivity, TArguments, TLog> :
    IActivityFactory<TActivity, TArguments, TLog>
    where TActivity : class, IExecuteActivity<TArguments>, ICompensateActivity<TLog>
    where TArguments : class
    where TLog : class
{
    readonly ICompensateActivityFactory<TActivity, TLog> _compensateFactory;
    readonly IExecuteActivityFactory<TActivity, TArguments> _executeFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="executeFactory">The execute factory value.</param>
    /// <param name="compensateFactory">The compensate factory value.</param>
    public FactoryMethodActivityFactory(Func<TArguments, TActivity> executeFactory,
        Func<TLog, TActivity> compensateFactory)
    {
        _executeFactory = new FactoryMethodExecuteActivityFactory<TActivity, TArguments>(executeFactory);
        _compensateFactory = new FactoryMethodCompensateActivityFactory<TActivity, TLog>(compensateFactory);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(ExecuteContext<TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next, CancellationToken cancellationToken = default)
    {
        return _executeFactory.ExecuteAsync(context, next, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the compensate operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CompensateAsync(CompensateContext<TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default)
    {
        return _compensateFactory.CompensateAsync(context, next, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("factoryMethod");
    }
}
