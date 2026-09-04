using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

public class FactoryMethodActivityFactory<TActivity, TArguments, TLog> :
    IActivityFactory<TActivity, TArguments, TLog>
    where TActivity : class, IExecuteActivity<TArguments>, ICompensateActivity<TLog>
    where TArguments : class
    where TLog : class
{
    readonly ICompensateActivityFactory<TActivity, TLog> _compensateFactory;
    readonly IExecuteActivityFactory<TActivity, TArguments> _executeFactory;

    public FactoryMethodActivityFactory(Func<TArguments, TActivity> executeFactory,
        Func<TLog, TActivity> compensateFactory)
    {
        _executeFactory = new FactoryMethodExecuteActivityFactory<TActivity, TArguments>(executeFactory);
        _compensateFactory = new FactoryMethodCompensateActivityFactory<TActivity, TLog>(compensateFactory);
    }

    public Task ExecuteAsync(ExecuteContext<TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next, CancellationToken cancellationToken = default)
    {
        return _executeFactory.ExecuteAsync(context, next, cancellationToken: cancellationToken);
    }

    public Task CompensateAsync(CompensateContext<TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default)
    {
        return _compensateFactory.CompensateAsync(context, next, cancellationToken: cancellationToken);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("factoryMethod");
    }
}
