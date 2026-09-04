using System;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.MessageJournal.Observers;

internal sealed class MessageJournalPublishObserver : IPublishObserver
{
    private readonly MessageJournalWriter _writer;

    public MessageJournalPublishObserver(MessageJournalWriter writer)
    {
        _writer = writer;
    }

    public Task PrePublish<T>(PublishContext<T> context)
        where T : class => Task.CompletedTask;

    public Task PostPublish<T>(PublishContext<T> context)
        where T : class => _writer.ObserveAsync(
            MessageJournalOperation.Publish,
            MessageJournalOutcome.Succeeded,
            context.CancellationToken,
            () => MessageJournalCaptureFactory.CreateSend(
                context,
                MessageJournalOperation.Publish,
                MessageJournalOutcome.Succeeded,
                exception: null));

    public Task PublishFault<T>(PublishContext<T> context, Exception exception)
        where T : class => _writer.ObserveAsync(
            MessageJournalOperation.Publish,
            MessageJournalOutcome.Faulted,
            context.CancellationToken,
            () => MessageJournalCaptureFactory.CreateSend(
                context,
                MessageJournalOperation.Publish,
                MessageJournalOutcome.Faulted,
                exception));
}
