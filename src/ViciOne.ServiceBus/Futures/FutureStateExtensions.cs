using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// Provides extension methods for future state.
/// </summary>
public static class FutureStateExtensions
{
    /// <summary>
    /// Gets command.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static T? GetCommand<T>(this BehaviorContext<FutureState> context)
        where T : class
    {
        return context.Saga.Command != null && context.Saga.Command.HasMessageType<T>()
            ? context.SerializerContext.DeserializeObject<T>(context.Saga.Command.Message)
            : null;
    }

    /// <summary>
    /// Performs the to object operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public static T? ToObject<T>(this BehaviorContext<FutureState> context, FutureMessage message)
        where T : class
    {
        return message != null && message.HasMessageType<T>()
            ? context.SerializerContext.DeserializeObject<T>(message.Message)
            : null;
    }

    /// <summary>
    /// Creates future message.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public static FutureMessage CreateFutureMessage<T>(this BehaviorContext<FutureState> context, T message)
        where T : class
    {
        IDictionary<string, object> dictionary = context.SerializerContext.ToDictionary(message);

        return new FutureMessage(dictionary, MessageTypeCache<T>.MessageTypeNames);
    }

    /// <summary>
    /// Performs the select results operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static IEnumerable<T> SelectResults<T>(this BehaviorContext<FutureState> context)
        where T : class
    {
        return context.Saga.HasResults()
            ? context.Saga.Results.Select(x => context.ToObject<T>(x.Value)).OfType<T>()
            : Enumerable.Empty<T>();
    }

    /// <summary>
    /// Adds subscription to the configuration.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public static void AddSubscription(this BehaviorContext<FutureState> context)
    {
        if (context.ResponseAddress == null)
            return;

        context.Saga.Subscriptions.Add(new FutureSubscription(context.ResponseAddress, context.RequestId));
    }

    /// <summary>
    /// Sets result.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="id">The id value.</param>
    /// <param name="factory">The factory value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<TResult> SetResultAsync<T, TResult>(this BehaviorContext<FutureState, T> context, Guid id,
        AsyncEventMessageFactory<FutureState, T, TResult> factory, CancellationToken cancellationToken = default)
        where T : class
        where TResult : class
    {
        cancellationToken.ThrowIfCancellationRequested(); if (!context.Saga.Completed.HasValue)
            SetCompleted(context, id);

        var result = await factory(context).ConfigureAwait(false);

        context.Saga.Results[id] = context.CreateFutureMessage(result);

        return result;
    }

    /// <summary>
    /// Sets result.
    /// </summary>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="id">The id value.</param>
    /// <param name="factory">The factory value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<TResult> SetResultAsync<TResult>(this BehaviorContext<FutureState> context, Guid id,
        AsyncEventMessageFactory<FutureState, TResult> factory, CancellationToken cancellationToken = default)
        where TResult : class
    {
        cancellationToken.ThrowIfCancellationRequested(); if (!context.Saga.Completed.HasValue)
            SetCompleted(context, id);

        var result = await factory(context).ConfigureAwait(false);

        context.Saga.Results[id] = context.CreateFutureMessage(result);

        return result;
    }

    /// <summary>
    /// Sets result.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="id">The id value.</param>
    /// <param name="factory">The factory value.</param>
    public static void SetResult<T, TResult>(this BehaviorContext<FutureState, T> context, Guid id, EventMessageFactory<FutureState, T, TResult> factory)
        where T : class
        where TResult : class
    {
        if (!context.Saga.Completed.HasValue)
            SetCompleted(context, id);

        var result = factory(context);

        context.Saga.Results[id] = context.CreateFutureMessage(result);
    }

    /// <summary>
    /// Sets result.
    /// </summary>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="id">The id value.</param>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static TResult SetResult<TResult>(this BehaviorContext<FutureState> context, Guid id, EventMessageFactory<FutureState, TResult> factory)
        where TResult : class
    {
        if (!context.Saga.Completed.HasValue)
            SetCompleted(context, id);

        var result = factory(context);

        context.Saga.Results[id] = context.CreateFutureMessage(result);

        return result;
    }

    /// <summary>
    /// Sets result.
    /// </summary>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="id">The id value.</param>
    /// <param name="result">The result value.</param>
    public static void SetResult<TResult>(this BehaviorContext<FutureState> context, Guid id, TResult result)
        where TResult : class
    {
        if (!context.Saga.Completed.HasValue)
            SetCompleted(context, id);

        context.Saga.Results[id] = context.CreateFutureMessage(result);
    }

    /// <summary>
    /// Sets completed.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="id">The id value.</param>
    public static void SetCompleted(this BehaviorContext<FutureState> context, Guid id)
    {
        var timestamp = context.SentTime ?? context.GetUtcDateTime();

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

    /// <summary>
    /// Sets faulted.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="id">The id value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    public static void SetFaulted(this BehaviorContext<FutureState> context, Guid id, DateTimeOffset? timestamp = default)
    {
        timestamp ??= context.SentTime ?? context.GetUtcDateTime();

        var future = context.Saga;

        if (future.HasPending())
            future.Pending?.Remove(id);

        future.Faulted ??= timestamp;
    }

    /// <summary>
    /// Sets fault.
    /// </summary>
    /// <typeparam name="TFault">The t fault type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="id">The id value.</param>
    /// <param name="fault">The fault value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    public static void SetFault<TFault>(this BehaviorContext<FutureState> context, Guid id, TFault fault, DateTimeOffset? timestamp = default)
        where TFault : class
    {
        SetFaulted(context, id, timestamp);

        context.Saga.Faults[id] = context.CreateFutureMessage(fault);
    }

    /// <summary>
    /// Sets fault.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TFault">The t fault type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="id">The id value.</param>
    /// <param name="factory">The factory value.</param>
    public static void SetFault<T, TFault>(this BehaviorContext<FutureState, T> context, Guid id, EventMessageFactory<FutureState, T, TFault> factory)
        where T : class
        where TFault : class
    {
        SetFaulted(context, id);

        var result = factory(context);

        context.Saga.Faults[id] = context.CreateFutureMessage(result);
    }

    /// <summary>
    /// Sets fault.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TFault">The t fault type.</typeparam>
    /// <param name="future">The future value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="id">The id value.</param>
    /// <param name="factory">The factory value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<TFault> SetFaultAsync<T, TFault>(this FutureState future, BehaviorContext<FutureState, T> context, Guid id,
        AsyncEventMessageFactory<FutureState, T, TFault> factory, CancellationToken cancellationToken = default)
        where T : class
        where TFault : class
    {
        cancellationToken.ThrowIfCancellationRequested(); var timestamp = context.SentTime ?? context.GetUtcDateTime();

        if (future.HasPending())
            future.Pending?.Remove(id);

        future.Faulted ??= timestamp;

        var fault = await factory(context).ConfigureAwait(false);

        future.Faults[id] = context.CreateFutureMessage(fault);

        return fault;
    }

    /// <summary>
    /// Attempts to get result.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="id">The id value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryGetResult<T>(this BehaviorContext<FutureState> context, Guid id, [NotNullWhen(true)] out T? result)
        where T : class
    {
        if (context.Saga.HasResults() && context.Saga.Results.TryGetValue(id, out var message))
        {
            result = context.ToObject<T>(message);
            return result != default;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to get fault.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="id">The id value.</param>
    /// <param name="fault">The fault value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryGetFault<T>(this BehaviorContext<FutureState> context, Guid id, [NotNullWhen(true)] out T? fault)
        where T : class
    {
        if (context.Saga.HasFaults() && context.Saga.Faults.TryGetValue(id, out var message))
        {
            fault = context.ToObject<T>(message);
            return fault != default;
        }

        fault = default;
        return false;
    }
}
