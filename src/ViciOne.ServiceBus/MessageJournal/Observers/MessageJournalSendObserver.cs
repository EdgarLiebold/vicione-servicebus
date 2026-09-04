using System;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.MessageJournal.Observers;

internal sealed class MessageJournalSendObserver : ISendObserver
{
    private readonly MessageJournalWriter _writer;

    public MessageJournalSendObserver(MessageJournalWriter writer)
    {
        _writer = writer;
    }

    public Task PreSendAsync<T>(SendContext<T> context)
        where T : class => Task.CompletedTask;

    public Task PostSendAsync<T>(SendContext<T> context)
        where T : class => _writer.ObserveAsync(
            MessageJournalOperation.Send,
            MessageJournalOutcome.Succeeded,
            context.CancellationToken,
            () => MessageJournalCaptureFactory.CreateSend(
                context,
                MessageJournalOperation.Send,
                MessageJournalOutcome.Succeeded,
                exception: null));

    public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
        where T : class => _writer.ObserveAsync(
            MessageJournalOperation.Send,
            MessageJournalOutcome.Faulted,
            context.CancellationToken,
            () => MessageJournalCaptureFactory.CreateSend(
                context,
                MessageJournalOperation.Send,
                MessageJournalOutcome.Faulted,
                exception));
}
