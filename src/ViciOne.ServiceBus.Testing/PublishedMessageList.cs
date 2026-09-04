using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a published message list implementation.
/// </summary>
public class PublishedMessageList :
    AsyncElementList<IPublishedMessage>,
    IPublishedMessageList
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    public PublishedMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public PublishedMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<IPublishedMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return Select(x => x is IPublishedMessage<T>, cancellationToken).Cast<IPublishedMessage<T>>();
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<IPublishedMessage<T>> Select<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add(filter);

        return Select(message => messageFilter.Any(message), cancellationToken).Cast<IPublishedMessage<T>>();
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="apply">The apply value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IAsyncEnumerable<IPublishedMessage> SelectAsync(Action<PublishedMessageFilter> apply, CancellationToken cancellationToken = default)
    {
        var messageFilter = new PublishedMessageFilter();
        apply?.Invoke(messageFilter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IAsyncEnumerable<IPublishedMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add<T>();

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .SelectAsync<IPublishedMessage, IPublishedMessage<T>>(cancellationToken);
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IAsyncEnumerable<IPublishedMessage<T>> SelectAsync<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add(filter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .SelectAsync<IPublishedMessage, IPublishedMessage<T>>(cancellationToken);
    }

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="apply">The apply value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<bool> AnyAsync(Action<PublishedMessageFilter>? apply = default, CancellationToken cancellationToken = default)
    {
        var messageFilter = new PublishedMessageFilter();
        apply?.Invoke(messageFilter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<bool> AnyAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add<T>();

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<bool> AnyAsync<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add(filter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    public void Add<T>(PublishContext<T> context)
        where T : class
    {
        Add(new PublishedMessage<T>(context, null, TimeProvider));
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public void Add<T>(PublishContext<T> context, Exception exception)
        where T : class
    {
        Add(new PublishedMessage<T>(context, exception, TimeProvider));
    }
}
