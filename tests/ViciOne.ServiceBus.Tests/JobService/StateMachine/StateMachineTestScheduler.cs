using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Tests.JobService.StateMachine;

internal sealed class StateMachineTestScheduler(TimeProvider timeProvider) : IMessageScheduler
{
    public TimeProvider TimeProvider { get; } = timeProvider;

    public List<ScheduledCall> Scheduled { get; } = [];

    public List<Guid> Canceled { get; } = [];

    public void Attach<T>(ConsumeContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        MessageSchedulerContext schedulerContext = DispatchProxy.Create<MessageSchedulerContext, SchedulerContextProxy>();
        ((SchedulerContextProxy)(object)schedulerContext).Configure(this, context.Advanced().ReceiveContext.InputAddress);
        context.AddOrUpdatePayload(() => schedulerContext, _ => schedulerContext);
    }

    public Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(
        Uri destination,
        DateTimeOffset dueAt,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class => ScheduleAsync(destination, dueAt, message, cancellationToken);

    public Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(
        Uri destination,
        DateTimeOffset dueAt,
        TMessage message,
        ScheduleOptions options,
        CancellationToken cancellationToken = default)
        where TMessage : class => ScheduleAsync(destination, dueAt, message, cancellationToken);

    public Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(
        Uri destination,
        TimeSpan delay,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class => ScheduleAsync(destination, TimeProvider.GetUtcNow() + delay, message, cancellationToken);

    public Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(
        Uri destination,
        TimeSpan delay,
        TMessage message,
        ScheduleOptions options,
        CancellationToken cancellationToken = default)
        where TMessage : class => ScheduleAsync(destination, TimeProvider.GetUtcNow() + delay, message, cancellationToken);

    public Task<ScheduledMessage<TMessage>> SchedulePublishAsync<TMessage>(
        DateTimeOffset dueAt,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class => ScheduleAsync(new Uri("loopback://localhost/publish"), dueAt, message, cancellationToken);

    public Task<ScheduledMessage<TMessage>> SchedulePublishAsync<TMessage>(
        TimeSpan delay,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class => ScheduleAsync(
            new Uri("loopback://localhost/publish"),
            TimeProvider.GetUtcNow() + delay,
            message,
            cancellationToken);

    public Task CancelScheduledSendAsync(ScheduledMessage scheduled, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scheduled);
        cancellationToken.ThrowIfCancellationRequested();
        Canceled.Add(scheduled.TokenId);
        return Task.CompletedTask;
    }

    public Task<ScheduledMessage<TMessage>> ScheduleFromContextAsync<TMessage>(
        Uri destination,
        DateTimeOffset dueAt,
        TMessage message,
        CancellationToken cancellationToken)
        where TMessage : class => ScheduleAsync(destination, dueAt, message, cancellationToken);

    public Task CancelFromContextAsync(Uri destination, Guid tokenId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        cancellationToken.ThrowIfCancellationRequested();
        Canceled.Add(tokenId);
        return Task.CompletedTask;
    }

    private Task<ScheduledMessage<TMessage>> ScheduleAsync<TMessage>(
        Uri destination,
        DateTimeOffset dueAt,
        TMessage message,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();
        Guid tokenId = NewId.NextGuid();
        Scheduled.Add(new ScheduledCall(tokenId, dueAt, destination, message));
        return Task.FromResult<ScheduledMessage<TMessage>>(
            new ScheduledMessageHandle<TMessage>(tokenId, dueAt, destination, message));
    }

    internal sealed record ScheduledCall(Guid TokenId, DateTimeOffset DueAt, Uri Destination, object Message);

    private class SchedulerContextProxy : DispatchProxy
    {
        private StateMachineTestScheduler _scheduler = null!;
        private Uri _inputAddress = null!;

        public void Configure(StateMachineTestScheduler scheduler, Uri inputAddress)
        {
            _scheduler = scheduler;
            _inputAddress = inputAddress;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            args ??= [];

            if (targetMethod.Name == "get_SchedulerFactory")
                return new MessageSchedulerFactory(_ => _scheduler);
            if (targetMethod.Name == "get_TimeProvider")
                return _scheduler.TimeProvider;
            if (targetMethod.Name == nameof(MessageSchedulerContext.ScheduleSendAsync)
                && targetMethod.IsGenericMethod
                && args.Length >= 3
                && args[0] is DateTimeOffset dueAt)
            {
                Type messageType = targetMethod.GetGenericArguments()[0];
                CancellationToken cancellationToken = args.OfType<CancellationToken>().SingleOrDefault();
                MethodInfo method = typeof(StateMachineTestScheduler)
                    .GetMethod(nameof(ScheduleFromContextAsync))!
                    .MakeGenericMethod(messageType);
                return method.Invoke(_scheduler, [_inputAddress, dueAt, args[1]!, cancellationToken]);
            }
            if (targetMethod.Name == nameof(IAdvancedMessageScheduler.CancelScheduledSendAsync)
                && args is [Uri destination, Guid tokenId, CancellationToken cancelToken])
            {
                return _scheduler.CancelFromContextAsync(destination, tokenId, cancelToken);
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }
}
