using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a sent message list implementation.
/// </summary>
public class SentMessageList :
    AsyncElementList<ISentMessage>,
    ISentMessageList
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    public SentMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public SentMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ISentMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return Select(x => x is ISentMessage<T>, cancellationToken).Cast<ISentMessage<T>>();
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ISentMessage<T>> Select<T>(FilterDelegate<ISentMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new SentMessageFilter();
        messageFilter.Includes.Add(filter);

        return Select(message => messageFilter.Any(message), cancellationToken).Cast<ISentMessage<T>>();
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="apply">The apply value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IAsyncEnumerable<ISentMessage> SelectAsync(Action<SentMessageFilter> apply, CancellationToken cancellationToken = default)
    {
        var messageFilter = new SentMessageFilter();
        apply?.Invoke(messageFilter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IAsyncEnumerable<ISentMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new SentMessageFilter();
        messageFilter.Includes.Add<T>();

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .SelectAsync<ISentMessage, ISentMessage<T>>(cancellationToken);
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IAsyncEnumerable<ISentMessage<T>> SelectAsync<T>(FilterDelegate<ISentMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new SentMessageFilter();
        messageFilter.Includes.Add(filter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .SelectAsync<ISentMessage, ISentMessage<T>>(cancellationToken);
    }

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="apply">The apply value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<bool> AnyAsync(Action<SentMessageFilter>? apply = default, CancellationToken cancellationToken = default)
    {
        var messageFilter = new SentMessageFilter();
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
        var messageFilter = new SentMessageFilter();
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
    public Task<bool> AnyAsync<T>(FilterDelegate<ISentMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new SentMessageFilter();
        messageFilter.Includes.Add(filter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    public void Add<T>(SendContext<T> context)
        where T : class
    {
        Add(new SentMessage<T>(context, null, TimeProvider));
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public void Add<T>(SendContext<T> context, Exception exception)
        where T : class
    {
        Add(new SentMessage<T>(context, exception, TimeProvider));
    }
}
