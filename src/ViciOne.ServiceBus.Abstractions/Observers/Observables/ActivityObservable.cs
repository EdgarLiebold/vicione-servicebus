using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

public class ActivityObservable :
    Connectable<IActivityObserver>,
    IActivityObserver
{
    public Task PreExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        return ForEachAsync(x => x.PreExecuteAsync(context));
    }

    public Task PostExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        return ForEachAsync(x => x.PostExecuteAsync(context));
    }

    public Task ExecuteFaultAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context, Exception exception)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        return ForEachAsync(x => x.ExecuteFaultAsync(context, exception));
    }

    public Task PreCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        return ForEachAsync(x => x.PreCompensateAsync(context));
    }

    public Task PostCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        return ForEachAsync(x => x.PostCompensateAsync(context));
    }

    public Task CompensateFailAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context, Exception exception)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        return ForEachAsync(x => x.CompensateFailAsync(context, exception));
    }
}
