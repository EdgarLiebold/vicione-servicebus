#nullable enable
namespace ViciOne.ServiceBus.MessageJournal.Observers;

using System;
using System.Threading.Tasks;

internal sealed class MessageJournalConsumeObserver : IConsumeObserver
{
    private readonly MessageJournalWriter _writer;

    public MessageJournalConsumeObserver(MessageJournalWriter writer)
    {
        _writer = writer;
    }

    public Task PreConsume<T>(ConsumeContext<T> context)
        where T : class => Task.CompletedTask;

    public Task PostConsume<T>(ConsumeContext<T> context)
        where T : class => _writer.ObserveAsync(
            MessageJournalOperation.Consume,
            MessageJournalOutcome.Succeeded,
            context.CancellationToken,
            () => MessageJournalCaptureFactory.CreateConsume(
                context,
                MessageJournalOutcome.Succeeded,
                exception: null));

    public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception)
        where T : class => _writer.ObserveAsync(
            MessageJournalOperation.Consume,
            MessageJournalOutcome.Faulted,
            context.CancellationToken,
            () => MessageJournalCaptureFactory.CreateConsume(
                context,
                MessageJournalOutcome.Faulted,
                exception));
}
