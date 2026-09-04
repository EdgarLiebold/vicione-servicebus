using System;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.MessageJournal.Observers;
using ViciOne.ServiceBus.Util;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for message journal configuration.
/// </summary>
public static class MessageJournalConfigurationExtensions
{
    /// <summary>
    /// Explicitly connects send, publish and consume journal observers. Without this call the
    /// MessageJournal capability has no runtime object and no resource cost.
    /// </summary>
    public static ConnectHandle ConnectMessageJournal<TConnector>(
        this TConnector connector,
        IMessageJournalStore store,
        IMessageJournalPolicy policy,
        MessageJournalOptions options)
        where TConnector : ISendObserverConnector, IPublishObserverConnector, IConsumeObserverConnector
    {
        ArgumentNullException.ThrowIfNull(connector);

        var writer = new MessageJournalWriter(store, policy, options);

        return ConnectAtomically(
            () => connector.ConnectSendObserver(new MessageJournalSendObserver(writer)),
            () => connector.ConnectPublishObserver(new MessageJournalPublishObserver(writer)),
            () => connector.ConnectConsumeObserver(new MessageJournalConsumeObserver(writer)));
    }

    /// <summary>
    /// Explicitly connects only outgoing send and publish journal observers.
    /// </summary>
    public static ConnectHandle ConnectOutgoingMessageJournal<TConnector>(
        this TConnector connector,
        IMessageJournalStore store,
        IMessageJournalPolicy policy,
        MessageJournalOptions options)
        where TConnector : ISendObserverConnector, IPublishObserverConnector
    {
        ArgumentNullException.ThrowIfNull(connector);

        var writer = new MessageJournalWriter(store, policy, options);

        return ConnectAtomically(
            () => connector.ConnectSendObserver(new MessageJournalSendObserver(writer)),
            () => connector.ConnectPublishObserver(new MessageJournalPublishObserver(writer)));
    }

    /// <summary>
    /// Explicitly connects only a consume journal observer.
    /// </summary>
    public static ConnectHandle ConnectConsumeMessageJournal(
        this IConsumeObserverConnector connector,
        IMessageJournalStore store,
        IMessageJournalPolicy policy,
        MessageJournalOptions options)
    {
        ArgumentNullException.ThrowIfNull(connector);

        var writer = new MessageJournalWriter(store, policy, options);
        return connector.ConnectConsumeObserver(new MessageJournalConsumeObserver(writer));
    }

    private static ConnectHandle ConnectAtomically(params Func<ConnectHandle>[] connect)
    {
        var handles = new ConnectHandle[connect.Length];
        var connected = 0;

        try
        {
            for (; connected < connect.Length; connected++)
            {
                handles[connected] = connect[connected]()
                    ?? throw new InvalidOperationException("An observer connector returned no connection handle.");
            }

            return new MultipleConnectHandle(handles);
        }
        catch
        {
            for (var index = connected - 1; index >= 0; index--)
            {
                try
                {
                    handles[index].Disconnect();
                }
                catch (Exception)
                {
                    // Preserve the connection failure while still unwinding every earlier observer.
                }
            }

            throw;
        }
    }
}
