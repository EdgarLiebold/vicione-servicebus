using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Stores a list of sent message values.</summary>
public class SentMessageList :
    AsyncElementList<ISentMessage>,
    ISentMessageList
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="testCompleted">The test completed.</param>
    public SentMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="testCompleted">The test completed.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public SentMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    public IEnumerable<ISentMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return Select(x => x is ISentMessage<T>, cancellationToken).Cast<ISentMessage<T>>();
    }

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    public IEnumerable<ISentMessage<T>> Select<T>(FilterDelegate<ISentMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new SentMessageFilter();
        messageFilter.Includes.Add(filter);

        return Select(message => messageFilter.Any(message), cancellationToken).Cast<ISentMessage<T>>();
    }

    /// <summary>Selects the matching value.</summary>
    /// <param name="apply">The apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    public IAsyncEnumerable<ISentMessage> SelectAsync(Action<SentMessageFilter> apply, CancellationToken cancellationToken = default)
    {
        var messageFilter = new SentMessageFilter();
        apply?.Invoke(messageFilter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    public IAsyncEnumerable<ISentMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new SentMessageFilter();
        messageFilter.Includes.Add<T>();

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .SelectAsync<ISentMessage, ISentMessage<T>>(cancellationToken);
    }

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    public IAsyncEnumerable<ISentMessage<T>> SelectAsync<T>(FilterDelegate<ISentMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new SentMessageFilter();
        messageFilter.Includes.Add(filter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .SelectAsync<ISentMessage, ISentMessage<T>>(cancellationToken);
    }

    /// <summary>Selects any matching value.</summary>
    /// <param name="apply">The apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    public Task<bool> AnyAsync(Action<SentMessageFilter>? apply = default, CancellationToken cancellationToken = default)
    {
        var messageFilter = new SentMessageFilter();
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
        var messageFilter = new SentMessageFilter();
        messageFilter.Includes.Add<T>();

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>Selects any matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    public Task<bool> AnyAsync<T>(FilterDelegate<ISentMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new SentMessageFilter();
        messageFilter.Includes.Add(filter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    public void Add<T>(SendContext<T> context)
        where T : class
    {
        Add(new SentMessage<T>(context, null, TimeProvider));
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public void Add<T>(SendContext<T> context, Exception exception)
        where T : class
    {
        Add(new SentMessage<T>(context, exception, TimeProvider));
    }
}
