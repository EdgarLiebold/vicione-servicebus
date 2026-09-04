using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a received message list implementation.
/// </summary>
public class ReceivedMessageList :
    AsyncElementList<IReceivedMessage>,
    IReceivedMessageList
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    public ReceivedMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public ReceivedMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<IReceivedMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return Select(x => x is IReceivedMessage<T>, cancellationToken).Cast<IReceivedMessage<T>>();
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<IReceivedMessage<T>> Select<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new ReceivedMessageFilter();
        messageFilter.Includes.Add(filter);

        return Select(message => messageFilter.Any(message), cancellationToken).Cast<IReceivedMessage<T>>();
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="apply">The apply value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IAsyncEnumerable<IReceivedMessage> SelectAsync(Action<ReceivedMessageFilter> apply, CancellationToken cancellationToken = default)
    {
        var messageFilter = new ReceivedMessageFilter();
        apply?.Invoke(messageFilter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IAsyncEnumerable<IReceivedMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new ReceivedMessageFilter();
        messageFilter.Includes.Add<T>();

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .SelectAsync<IReceivedMessage, IReceivedMessage<T>>(cancellationToken);
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IAsyncEnumerable<IReceivedMessage<T>> SelectAsync<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new ReceivedMessageFilter();
        messageFilter.Includes.Add(filter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .SelectAsync<IReceivedMessage, IReceivedMessage<T>>(cancellationToken);
    }

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="apply">The apply value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<bool> AnyAsync(Action<ReceivedMessageFilter>? apply = default, CancellationToken cancellationToken = default)
    {
        var messageFilter = new ReceivedMessageFilter();
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
        var messageFilter = new ReceivedMessageFilter();
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
    public Task<bool> AnyAsync<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new ReceivedMessageFilter();
        messageFilter.Includes.Add(filter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    public void Add<T>(ConsumeContext<T> context)
        where T : class
    {
        Add(new ReceivedMessage<T>(context, null, TimeProvider));
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public void Add<T>(ConsumeContext<T> context, Exception exception)
        where T : class
    {
        Add(new ReceivedMessage<T>(context, exception, TimeProvider));
    }
}


/// <summary>
/// Provides a received message list implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ReceivedMessageList<T> :
    AsyncElementList<IReceivedMessage<T>>,
    IReceivedMessageList<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    public ReceivedMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public ReceivedMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<IReceivedMessage<T>> Select(CancellationToken cancellationToken = default)
    {
        return Select(x => true, cancellationToken);
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IAsyncEnumerable<IReceivedMessage<T>> SelectAsync(CancellationToken cancellationToken = default)
    {
        return SelectAsync(x => true, cancellationToken);
    }

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return AnyAsync(x => true, cancellationToken);
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Add(ConsumeContext<T> context)
    {
        Add(new ReceivedMessage<T>(context, null, TimeProvider));
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public void Add(ConsumeContext<T> context, Exception exception)
    {
        Add(new ReceivedMessage<T>(context, exception, TimeProvider));
    }
}
