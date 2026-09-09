using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>Dispatches runtime-typed response operations through cached generic converters.</summary>
public static class ResponseEndpointConverterCache
{
    static readonly ConcurrentDictionary<Type, Lazy<IResponseEndpointConverter>> Converters = new();

    /// <summary>Sends a response using an explicit runtime contract type.</summary>
    /// <param name="consumeContext">The consume context that sends the response.</param>
    /// <param name="message">The response message.</param>
    /// <param name="messageType">The runtime response contract type.</param>
    /// <returns>A task that completes when the response is accepted by the transport.</returns>
    public static Task RespondAsync(ConsumeContext consumeContext, object message, Type messageType)
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(message);

        return GetConverter(messageType).RespondAsync(consumeContext, message);
    }

    /// <summary>Sends a response using an explicit runtime contract type and send-context pipe.</summary>
    /// <param name="consumeContext">The consume context that sends the response.</param>
    /// <param name="message">The response message.</param>
    /// <param name="messageType">The runtime response contract type.</param>
    /// <param name="pipe">The pipe that customizes the response send context.</param>
    /// <returns>A task that completes when the response is accepted by the transport.</returns>
    public static Task RespondAsync(ConsumeContext consumeContext, object message, Type messageType, IPipe<SendContext> pipe)
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return GetConverter(messageType).RespondAsync(consumeContext, message, pipe);
    }

    static IResponseEndpointConverter GetConverter(Type messageType)
    {
        ValidateMessageType(messageType);

        return Converters.GetOrAdd(messageType, CreateTypeConverter).Value;
    }

    static Lazy<IResponseEndpointConverter> CreateTypeConverter(Type type)
    {
        return new Lazy<IResponseEndpointConverter>(() => CreateConverter(type), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    static IResponseEndpointConverter CreateConverter(Type type)
    {
        return Activator.CreateInstance(typeof(ResponseEndpointConverter<>).MakeGenericType(type)) as IResponseEndpointConverter
            ?? throw new InvalidOperationException($"Unable to create a response converter for {TypeCache.GetShortName(type)}.");
    }

    static void ValidateMessageType(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        if (messageType.IsValueType || messageType.IsByRef || messageType.IsPointer || messageType.ContainsGenericParameters)
            throw new ArgumentException("The response contract must be a closed reference type.", nameof(messageType));
    }

    interface IResponseEndpointConverter
    {
        Task RespondAsync(ConsumeContext consumeContext, object message);

        Task RespondAsync(ConsumeContext consumeContext, object message, IPipe<SendContext> pipe);
    }

    sealed class ResponseEndpointConverter<TResponse> :
        IResponseEndpointConverter
        where TResponse : class
    {
        public Task RespondAsync(ConsumeContext consumeContext, object message)
        {
            if (message is TResponse typedResponse)
                return consumeContext.RespondAsync(typedResponse);

            throw new ArgumentException($"The response is not assignable to {TypeCache.GetShortName(typeof(TResponse))}.", nameof(message));
        }

        public Task RespondAsync(ConsumeContext consumeContext, object message, IPipe<SendContext> pipe)
        {
            if (message is TResponse typedResponse)
                return consumeContext.RespondAsync(typedResponse, pipe);

            throw new ArgumentException($"The response is not assignable to {TypeCache.GetShortName(typeof(TResponse))}.", nameof(message));
        }
    }
}
