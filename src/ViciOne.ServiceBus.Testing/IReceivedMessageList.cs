using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

public interface IReceivedMessageList :
    IAsyncElementList<IReceivedMessage>
{
    IEnumerable<IReceivedMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class;

    IEnumerable<IReceivedMessage<T>> Select<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;

    IAsyncEnumerable<IReceivedMessage> SelectAsync(Action<ReceivedMessageFilter> apply, CancellationToken cancellationToken = default);

    IAsyncEnumerable<IReceivedMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class;

    IAsyncEnumerable<IReceivedMessage<T>> SelectAsync<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;

    Task<bool> AnyAsync(Action<ReceivedMessageFilter>? apply = default, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync<T>(CancellationToken cancellationToken = default)
        where T : class;

    Task<bool> AnyAsync<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;
}


public interface IReceivedMessageList<out T> :
    IAsyncElementList<IReceivedMessage<T>>
    where T : class
{
    IEnumerable<IReceivedMessage<T>> Select(CancellationToken cancellationToken = default);

    IAsyncEnumerable<IReceivedMessage<T>> SelectAsync(CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
