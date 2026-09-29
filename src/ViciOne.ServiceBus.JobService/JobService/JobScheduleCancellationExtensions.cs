using System;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.JobService;

internal static class JobScheduleCancellationExtensions
{
    public static IEventActivityBinder<TSaga, TData> UnscheduleJob<TSaga, TData>(
        this IEventActivityBinder<TSaga, TData> source, ISchedule<TSaga> schedule)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(schedule);

        return source.IfElse(
            context => context.TryGetPayload(out MessageSchedulerContext? scheduler)
                && scheduler is IScheduleCancellationCapability
                    { CancellationMode: ScheduleCancellationMode.CallerSpecifiedTokenWithoutCancellation },
            delayed => delayed.Then(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                schedule.SetTokenId(context.Saga, null);
            }),
            cancellable => cancellable.Unschedule(schedule));
    }
}
