using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Serializes commands and outcomes and advances a future's durable state.</summary>
public static class FutureStateExtensions
{
    /// <summary>Deserializes the command that created the future when it implements the requested contract.</summary>
    /// <typeparam name="T">The requested command contract.</typeparam>
    /// <param name="context">The future state context that provides the stored command and serializer.</param>
    /// <returns>The stored command, or <see langword="null" /> when it does not implement the requested contract.</returns>
    public static T? GetCommand<T>(this IBehaviorContext<FutureState> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Saga.Command != null && context.Saga.Command.HasMessageType<T>()
            ? context.SerializerContext.DeserializeObject<T>(context.Saga.Command.Message)
            : null;
    }

    /// <summary>Deserializes a stored future message when it implements the requested contract.</summary>
    /// <typeparam name="T">The requested message contract.</typeparam>
    /// <param name="context">The future state context that provides the serializer.</param>
    /// <param name="message">The stored future message.</param>
    /// <returns>The deserialized message, or <see langword="null" /> when the contract is not supported.</returns>
    public static T? ToObject<T>(this IBehaviorContext<FutureState> context, FutureMessage message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        return message.HasMessageType<T>()
            ? context.SerializerContext.DeserializeObject<T>(message.Message)
            : null;
    }

    /// <summary>Serializes a message into the durable representation used by future state.</summary>
    /// <typeparam name="T">The message contract to serialize.</typeparam>
    /// <param name="context">The future state context that provides the serializer.</param>
    /// <param name="message">The message to serialize.</param>
    /// <returns>The serialized message and its supported contract URNs.</returns>
    public static FutureMessage CreateFutureMessage<T>(this IBehaviorContext<FutureState> context, T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        IDictionary<string, object> dictionary = context.SerializerContext.ToDictionary(message);

        return new FutureMessage(new Dictionary<string, object>(dictionary), [.. MessageTypeCache<T>.MessageTypeNames]);
    }

    /// <summary>Deserializes all stored successful results that implement the requested contract.</summary>
    /// <typeparam name="T">The requested result contract.</typeparam>
    /// <param name="context">The future state context that contains the stored results.</param>
    /// <returns>The matching successful results.</returns>
    public static IEnumerable<T> SelectResults<T>(this IBehaviorContext<FutureState> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Saga.HasResults()
            ? context.Saga.Results.Select(x => context.ToObject<T>(x.Value)).OfType<T>()
            : [];
    }

    /// <summary>Adds the response endpoint and optional request identifier as a future subscriber.</summary>
    /// <param name="context">The request context whose response address is subscribed.</param>
    public static void AddSubscription(this IBehaviorContext<FutureState> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.ResponseAddress == null)
            return;

        context.Saga.Subscriptions.Add(new FutureSubscription(context.ResponseAddress, context.RequestId));
    }

    /// <summary>Creates and stores a successful result for a correlated operation.</summary>
    /// <typeparam name="T">The event contract available to the result factory.</typeparam>
    /// <typeparam name="TResult">The successful result contract.</typeparam>
    /// <param name="context">The future event context used to create and serialize the result.</param>
    /// <param name="id">The completed operation identifier.</param>
    /// <param name="factory">The asynchronous result factory.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created result.</returns>
    public static async Task<TResult> SetResultAsync<T, TResult>(this IBehaviorContext<FutureState, T> context, Guid id,
        AsyncEventMessageFactory<FutureState, T, TResult> factory, CancellationToken cancellationToken = default)
        where T : class
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(factory);
        cancellationToken.ThrowIfCancellationRequested();
        var result = await factory(context).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(result);
        FutureMessage storedResult = context.CreateFutureMessage(result);

        if (!context.Saga.Completed.HasValue)
            SetCompleted(context, id);

        context.Saga.Results[id] = storedResult;

        return result;
    }

    /// <summary>Creates and stores a successful result for a correlated operation.</summary>
    /// <typeparam name="TResult">The successful result contract.</typeparam>
    /// <param name="context">The future state context used to create and serialize the result.</param>
    /// <param name="id">The completed operation identifier.</param>
    /// <param name="factory">The asynchronous result factory.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created result.</returns>
    public static async Task<TResult> SetResultAsync<TResult>(this IBehaviorContext<FutureState> context, Guid id,
        AsyncEventMessageFactory<FutureState, TResult> factory, CancellationToken cancellationToken = default)
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(factory);
        cancellationToken.ThrowIfCancellationRequested();
        var result = await factory(context).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(result);
        FutureMessage storedResult = context.CreateFutureMessage(result);

        if (!context.Saga.Completed.HasValue)
            SetCompleted(context, id);

        context.Saga.Results[id] = storedResult;

        return result;
    }

    /// <summary>Creates and stores a successful result for a correlated operation.</summary>
    /// <typeparam name="T">The event contract available to the result factory.</typeparam>
    /// <typeparam name="TResult">The successful result contract.</typeparam>
    /// <param name="context">The future event context used to create and serialize the result.</param>
    /// <param name="id">The completed operation identifier.</param>
    /// <param name="factory">The synchronous result factory.</param>
    /// <returns>The created result.</returns>
    public static TResult SetResult<T, TResult>(this IBehaviorContext<FutureState, T> context, Guid id,
        EventMessageFactory<FutureState, T, TResult> factory)
        where T : class
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(factory);
        var result = factory(context);
        ArgumentNullException.ThrowIfNull(result);
        FutureMessage storedResult = context.CreateFutureMessage(result);

        if (!context.Saga.Completed.HasValue)
            SetCompleted(context, id);

        context.Saga.Results[id] = storedResult;
        return result;
    }

    /// <summary>Creates and stores a successful result for a correlated operation.</summary>
    /// <typeparam name="TResult">The successful result contract.</typeparam>
    /// <param name="context">The future state context used to create and serialize the result.</param>
    /// <param name="id">The completed operation identifier.</param>
    /// <param name="factory">The synchronous result factory.</param>
    /// <returns>The created result.</returns>
    public static TResult SetResult<TResult>(this IBehaviorContext<FutureState> context, Guid id, EventMessageFactory<FutureState, TResult> factory)
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(factory);
        var result = factory(context);
        ArgumentNullException.ThrowIfNull(result);
        FutureMessage storedResult = context.CreateFutureMessage(result);

        if (!context.Saga.Completed.HasValue)
            SetCompleted(context, id);

        context.Saga.Results[id] = storedResult;

        return result;
    }

    /// <summary>Stores a successful result for a correlated operation.</summary>
    /// <typeparam name="TResult">The successful result contract.</typeparam>
    /// <param name="context">The future state context used to serialize the result.</param>
    /// <param name="id">The completed operation identifier.</param>
    /// <param name="result">The result to store.</param>
    public static void SetResult<TResult>(this IBehaviorContext<FutureState> context, Guid id, TResult result)
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);
        FutureMessage storedResult = context.CreateFutureMessage(result);

        if (!context.Saga.Completed.HasValue)
            SetCompleted(context, id);

        context.Saga.Results[id] = storedResult;
    }

    /// <summary>Marks one operation complete and completes the future when no work or fault remains.</summary>
    /// <param name="context">The future state context to update.</param>
    /// <param name="id">The completed operation identifier.</param>
    public static void SetCompleted(this IBehaviorContext<FutureState> context, Guid id)
    {
        ArgumentNullException.ThrowIfNull(context);
        var timestamp = context.SentTime ?? context.GetUtcNow();

        var future = context.Saga;

        if (future.HasPending())
        {
            future.Pending.Remove(id);

            if (!future.HasPending() && !future.HasFaults())
                future.Completed = timestamp;
        }
        else if (!future.HasFaults())
            future.Completed = timestamp;
    }

    /// <summary>Marks one operation faulted and records the future's first fault timestamp.</summary>
    /// <param name="context">The future state context to update.</param>
    /// <param name="id">The faulted operation identifier.</param>
    /// <param name="timestamp">The fault timestamp, or <see langword="null" /> to use the message or current time.</param>
    public static void SetFaulted(this IBehaviorContext<FutureState> context, Guid id, DateTimeOffset? timestamp = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        timestamp ??= context.SentTime ?? context.GetUtcNow();

        var future = context.Saga;

        if (future.HasPending())
            future.Pending?.Remove(id);

        future.Faulted ??= timestamp;
    }

    /// <summary>Marks one operation faulted and stores its serialized fault.</summary>
    /// <typeparam name="TFault">The fault contract.</typeparam>
    /// <param name="context">The future state context to update.</param>
    /// <param name="id">The faulted operation identifier.</param>
    /// <param name="fault">The fault to store.</param>
    /// <param name="timestamp">The fault timestamp, or <see langword="null" /> to use the message or current time.</param>
    public static void SetFault<TFault>(this IBehaviorContext<FutureState> context, Guid id, TFault fault, DateTimeOffset? timestamp = default)
        where TFault : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(fault);
        FutureMessage storedFault = context.CreateFutureMessage(fault);

        SetFaulted(context, id, timestamp);

        context.Saga.Faults[id] = storedFault;
    }

    /// <summary>Creates and stores a fault for a correlated operation.</summary>
    /// <typeparam name="T">The event contract available to the fault factory.</typeparam>
    /// <typeparam name="TFault">The fault contract.</typeparam>
    /// <param name="context">The future event context used to create and serialize the fault.</param>
    /// <param name="id">The faulted operation identifier.</param>
    /// <param name="factory">The synchronous fault factory.</param>
    /// <returns>The created fault.</returns>
    public static TFault SetFault<T, TFault>(this IBehaviorContext<FutureState, T> context, Guid id,
        EventMessageFactory<FutureState, T, TFault> factory)
        where T : class
        where TFault : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(factory);
        var result = factory(context);
        ArgumentNullException.ThrowIfNull(result);
        FutureMessage storedFault = context.CreateFutureMessage(result);

        SetFaulted(context, id);
        context.Saga.Faults[id] = storedFault;
        return result;
    }

    /// <summary>Creates and stores a fault for a correlated operation.</summary>
    /// <typeparam name="T">The event contract available to the fault factory.</typeparam>
    /// <typeparam name="TFault">The fault contract.</typeparam>
    /// <param name="context">The future event context used to create and serialize the fault.</param>
    /// <param name="id">The faulted operation identifier.</param>
    /// <param name="factory">The asynchronous fault factory.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created fault.</returns>
    public static async Task<TFault> SetFaultAsync<T, TFault>(this IBehaviorContext<FutureState, T> context, Guid id,
        AsyncEventMessageFactory<FutureState, T, TFault> factory, CancellationToken cancellationToken = default)
        where T : class
        where TFault : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(factory);
        cancellationToken.ThrowIfCancellationRequested();
        var fault = await factory(context).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(fault);
        FutureMessage storedFault = context.CreateFutureMessage(fault);

        SetFaulted(context, id);
        context.Saga.Faults[id] = storedFault;

        return fault;
    }

    /// <summary>Attempts to deserialize the successful result stored for an operation.</summary>
    /// <typeparam name="T">The requested result contract.</typeparam>
    /// <param name="context">The future state context that contains the stored results.</param>
    /// <param name="id">The operation identifier.</param>
    /// <param name="result">Receives the matching result when found and deserializable.</param>
    /// <returns><see langword="true" /> when a matching result was deserialized; otherwise, <see langword="false" />.</returns>
    public static bool TryGetResult<T>(this IBehaviorContext<FutureState> context, Guid id, [NotNullWhen(true)] out T? result)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Saga.HasResults() && context.Saga.Results.TryGetValue(id, out var message))
        {
            result = context.ToObject<T>(message);
            return result != default;
        }

        result = default;
        return false;
    }

    /// <summary>Attempts to deserialize the fault stored for an operation.</summary>
    /// <typeparam name="T">The requested fault contract.</typeparam>
    /// <param name="context">The future state context that contains the stored faults.</param>
    /// <param name="id">The operation identifier.</param>
    /// <param name="fault">Receives the matching fault when found and deserializable.</param>
    /// <returns><see langword="true" /> when a matching fault was deserialized; otherwise, <see langword="false" />.</returns>
    public static bool TryGetFault<T>(this IBehaviorContext<FutureState> context, Guid id, [NotNullWhen(true)] out T? fault)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Saga.HasFaults() && context.Saga.Faults.TryGetValue(id, out var message))
        {
            fault = context.ToObject<T>(message);
            return fault != default;
        }

        fault = default;
        return false;
    }
}
