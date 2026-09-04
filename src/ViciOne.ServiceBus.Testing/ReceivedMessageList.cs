using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

public class ReceivedMessageList :
    AsyncElementList<IReceivedMessage>,
    IReceivedMessageList
{
    public ReceivedMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    public ReceivedMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    public IEnumerable<IReceivedMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return Select(x => x is IReceivedMessage<T>, cancellationToken).Cast<IReceivedMessage<T>>();
    }

    public IEnumerable<IReceivedMessage<T>> Select<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new ReceivedMessageFilter();
        messageFilter.Includes.Add(filter);

        return Select(message => messageFilter.Any(message), cancellationToken).Cast<IReceivedMessage<T>>();
    }

    public IAsyncEnumerable<IReceivedMessage> SelectAsync(Action<ReceivedMessageFilter> apply, CancellationToken cancellationToken = default)
    {
        var messageFilter = new ReceivedMessageFilter();
        apply?.Invoke(messageFilter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken);
    }

    public IAsyncEnumerable<IReceivedMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new ReceivedMessageFilter();
        messageFilter.Includes.Add<T>();

        return SelectAsync(message => messageFilter.Any(message), cancellationToken).Select<IReceivedMessage, IReceivedMessage<T>>();
    }

    public IAsyncEnumerable<IReceivedMessage<T>> SelectAsync<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new ReceivedMessageFilter();
        messageFilter.Includes.Add(filter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken).Select<IReceivedMessage, IReceivedMessage<T>>();
    }

    public Task<bool> AnyAsync(Action<ReceivedMessageFilter>? apply = default, CancellationToken cancellationToken = default)
    {
        var messageFilter = new ReceivedMessageFilter();
        apply?.Invoke(messageFilter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    public Task<bool> AnyAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new ReceivedMessageFilter();
        messageFilter.Includes.Add<T>();

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    public Task<bool> AnyAsync<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new ReceivedMessageFilter();
        messageFilter.Includes.Add(filter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    public void Add<T>(ConsumeContext<T> context)
        where T : class
    {
        Add(new ReceivedMessage<T>(context, null, TimeProvider));
    }

    public void Add<T>(ConsumeContext<T> context, Exception exception)
        where T : class
    {
        Add(new ReceivedMessage<T>(context, exception, TimeProvider));
    }
}


public class ReceivedMessageList<T> :
    AsyncElementList<IReceivedMessage<T>>,
    IReceivedMessageList<T>
    where T : class
{
    public ReceivedMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    public ReceivedMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    public IEnumerable<IReceivedMessage<T>> Select(CancellationToken cancellationToken = default)
    {
        return Select(x => true, cancellationToken);
    }

    public IAsyncEnumerable<IReceivedMessage<T>> SelectAsync(CancellationToken cancellationToken = default)
    {
        return SelectAsync(x => true, cancellationToken);
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return AnyAsync(x => true, cancellationToken);
    }

    public void Add(ConsumeContext<T> context)
    {
        Add(new ReceivedMessage<T>(context, null, TimeProvider));
    }

    public void Add(ConsumeContext<T> context, Exception exception)
    {
        Add(new ReceivedMessage<T>(context, exception, TimeProvider));
    }
}
