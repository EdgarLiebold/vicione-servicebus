using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

public class PublishedMessageList :
    AsyncElementList<IPublishedMessage>,
    IPublishedMessageList
{
    public PublishedMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    public PublishedMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    public IEnumerable<IPublishedMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return Select(x => x is IPublishedMessage<T>, cancellationToken).Cast<IPublishedMessage<T>>();
    }

    public IEnumerable<IPublishedMessage<T>> Select<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add(filter);

        return Select(message => messageFilter.Any(message), cancellationToken).Cast<IPublishedMessage<T>>();
    }

    public IAsyncEnumerable<IPublishedMessage> SelectAsync(Action<PublishedMessageFilter> apply, CancellationToken cancellationToken = default)
    {
        var messageFilter = new PublishedMessageFilter();
        apply?.Invoke(messageFilter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken);
    }

    public IAsyncEnumerable<IPublishedMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add<T>();

        return SelectAsync(message => messageFilter.Any(message), cancellationToken).Select<IPublishedMessage, IPublishedMessage<T>>();
    }

    public IAsyncEnumerable<IPublishedMessage<T>> SelectAsync<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add(filter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken).Select<IPublishedMessage, IPublishedMessage<T>>();
    }

    public Task<bool> AnyAsync(Action<PublishedMessageFilter>? apply = default, CancellationToken cancellationToken = default)
    {
        var messageFilter = new PublishedMessageFilter();
        apply?.Invoke(messageFilter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    public Task<bool> AnyAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add<T>();

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    public Task<bool> AnyAsync<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add(filter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    public void Add<T>(PublishContext<T> context)
        where T : class
    {
        Add(new PublishedMessage<T>(context, null, TimeProvider));
    }

    public void Add<T>(PublishContext<T> context, Exception exception)
        where T : class
    {
        Add(new PublishedMessage<T>(context, exception, TimeProvider));
    }
}
