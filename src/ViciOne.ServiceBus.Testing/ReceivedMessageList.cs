using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Stores a list of received message values.</summary>
public class ReceivedMessageList :
    AsyncElementList<IReceivedMessage>,
    IReceivedMessageList
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="testCompleted">The test completed.</param>
    public ReceivedMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="testCompleted">The test completed.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public ReceivedMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    public IEnumerable<IReceivedMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return Select(x => x is IReceivedMessage<T>, cancellationToken).Cast<IReceivedMessage<T>>();
    }

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    public IEnumerable<IReceivedMessage<T>> Select<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new ReceivedMessageFilter();
        messageFilter.Includes.Add(filter);

        return Select(message => messageFilter.Any(message), cancellationToken).Cast<IReceivedMessage<T>>();
    }

    /// <summary>Selects the matching value.</summary>
    /// <param name="apply">The apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    public IAsyncEnumerable<IReceivedMessage> SelectAsync(Action<ReceivedMessageFilter> apply, CancellationToken cancellationToken = default)
    {
        var messageFilter = new ReceivedMessageFilter();
        apply?.Invoke(messageFilter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    public IAsyncEnumerable<IReceivedMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new ReceivedMessageFilter();
        messageFilter.Includes.Add<T>();

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .SelectAsync<IReceivedMessage, IReceivedMessage<T>>(cancellationToken);
    }

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    public IAsyncEnumerable<IReceivedMessage<T>> SelectAsync<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new ReceivedMessageFilter();
        messageFilter.Includes.Add(filter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .SelectAsync<IReceivedMessage, IReceivedMessage<T>>(cancellationToken);
    }

    /// <summary>Selects any matching value.</summary>
    /// <param name="apply">The apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    public Task<bool> AnyAsync(Action<ReceivedMessageFilter>? apply = default, CancellationToken cancellationToken = default)
    {
        var messageFilter = new ReceivedMessageFilter();
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
        var messageFilter = new ReceivedMessageFilter();
        messageFilter.Includes.Add<T>();

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>Selects any matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    public Task<bool> AnyAsync<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageFilter = new ReceivedMessageFilter();
        messageFilter.Includes.Add(filter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    public void Add<T>(ConsumeContext<T> context)
        where T : class
    {
        Add(new ReceivedMessage<T>(context, null, TimeProvider));
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public void Add<T>(ConsumeContext<T> context, Exception exception)
        where T : class
    {
        Add(new ReceivedMessage<T>(context, exception, TimeProvider));
    }
}


/// <summary>Stores a list of received message values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ReceivedMessageList<T> :
    AsyncElementList<IReceivedMessage<T>>,
    IReceivedMessageList<T>
    where T : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="testCompleted">The test completed.</param>
    public ReceivedMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="testCompleted">The test completed.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public ReceivedMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    /// <summary>Selects the matching value.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    public IEnumerable<IReceivedMessage<T>> Select(CancellationToken cancellationToken = default)
    {
        return Select(x => true, cancellationToken);
    }

    /// <summary>Selects the matching value.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    public IAsyncEnumerable<IReceivedMessage<T>> SelectAsync(CancellationToken cancellationToken = default)
    {
        return SelectAsync(x => true, cancellationToken);
    }

    /// <summary>Selects any matching value.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return AnyAsync(x => true, cancellationToken);
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Add(ConsumeContext<T> context)
    {
        Add(new ReceivedMessage<T>(context, null, TimeProvider));
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public void Add(ConsumeContext<T> context, Exception exception)
    {
        Add(new ReceivedMessage<T>(context, exception, TimeProvider));
    }
}
