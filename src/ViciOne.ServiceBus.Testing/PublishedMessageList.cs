using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Stores a list of published message values.</summary>
public class PublishedMessageList :
    AsyncElementList<IPublishedMessage>,
    IPublishedMessageList
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="testCompleted">The test completed.</param>
    public PublishedMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="testCompleted">The test completed.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public PublishedMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    public IEnumerable<IPublishedMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return Select(x => x is IPublishedMessage<T>, cancellationToken).Cast<IPublishedMessage<T>>();
    }

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    public IEnumerable<IPublishedMessage<T>> Select<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add(filter);

        return Select(message => messageFilter.Any(message), cancellationToken).Cast<IPublishedMessage<T>>();
    }

    /// <summary>Selects the matching value.</summary>
    /// <param name="apply">The apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    public IAsyncEnumerable<IPublishedMessage> SelectAsync(Action<PublishedMessageFilter> apply, CancellationToken cancellationToken = default)
    {
        var messageFilter = new PublishedMessageFilter();
        apply?.Invoke(messageFilter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    public IAsyncEnumerable<IPublishedMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add<T>();

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .SelectAsync<IPublishedMessage, IPublishedMessage<T>>(cancellationToken);
    }

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    public IAsyncEnumerable<IPublishedMessage<T>> SelectAsync<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add(filter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .SelectAsync<IPublishedMessage, IPublishedMessage<T>>(cancellationToken);
    }

    /// <summary>Selects any matching value.</summary>
    /// <param name="apply">The apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    public Task<bool> AnyAsync(Action<PublishedMessageFilter>? apply = default, CancellationToken cancellationToken = default)
    {
        var messageFilter = new PublishedMessageFilter();
        apply?.Invoke(messageFilter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>Selects any matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    public Task<bool> AnyAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add<T>();

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>Selects any matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    public Task<bool> AnyAsync<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add(filter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    public void Add<T>(PublishContext<T> context)
        where T : class
    {
        Add(new PublishedMessage<T>(context, null, TimeProvider));
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public void Add<T>(PublishContext<T> context, Exception exception)
        where T : class
    {
        Add(new PublishedMessage<T>(context, exception, TimeProvider));
    }
}
