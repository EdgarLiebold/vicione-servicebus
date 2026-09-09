using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// Exposes a received response message together with its messaging context.
/// </summary>
public interface Response :
    MessageContext
{
    /// <summary>Gets the received response message.</summary>
    object Message { get; }
}


/// <summary>Exposes a strongly typed response message together with its messaging context.</summary>
/// <typeparam name="TResponse">The response message contract.</typeparam>
public interface Response<out TResponse> :
    Response
    where TResponse : class
{
    /// <summary>Gets the received response message.</summary>
    new TResponse Message { get; }
}


/// <summary>
/// Represents the completed branch of a request that accepts two response contracts while preserving both branch tasks.
/// </summary>
/// <typeparam name="TResponse1">The first accepted response contract.</typeparam>
/// <typeparam name="TResponse2">The second accepted response contract.</typeparam>
public readonly struct Response<TResponse1, TResponse2> :
    Response
    where TResponse1 : class
    where TResponse2 : class
{
    readonly Response<TResponse1>? _response1;
    readonly Response<TResponse2>? _response2;
    readonly Response? _response;
    readonly Task<Response<TResponse1>>? _response1Task;
    readonly Task<Response<TResponse2>>? _response2Task;

    /// <summary>Creates a two-contract response from branch tasks when at least one branch completed successfully.</summary>
    /// <param name="response1">The first response-contract task.</param>
    /// <param name="response2">The second response-contract task.</param>
    public Response(Task<Response<TResponse1>> response1, Task<Response<TResponse2>> response2)
    {
        ArgumentNullException.ThrowIfNull(response1);
        ArgumentNullException.ThrowIfNull(response2);

        _response1Task = response1;
        _response2Task = response2;

        _response1 = response1.Status == TaskStatus.RanToCompletion ? response1.GetAwaiter().GetResult() : default;
        _response2 = response2.Status == TaskStatus.RanToCompletion ? response2.GetAwaiter().GetResult() : default;

        _response = _response1 as Response ?? _response2 ?? throw new ArgumentException("At least one response must have completed");
    }

    /// <summary>Gets the first response branch when it completed successfully.</summary>
    /// <param name="result">Receives the first response branch when available.</param>
    /// <returns><see langword="true"/> when the first branch completed successfully; otherwise, <see langword="false"/>.</returns>
    public bool Is([NotNullWhen(true)] out Response<TResponse1>? result)
    {
        result = _response1;

        return result != default;
    }

    /// <summary>Gets the second response branch when it completed successfully.</summary>
    /// <param name="result">Receives the second response branch when available.</param>
    /// <returns><see langword="true"/> when the second branch completed successfully; otherwise, <see langword="false"/>.</returns>
    public bool Is([NotNullWhen(true)] out Response<TResponse2>? result)
    {
        result = _response2;

        return result != default;
    }

    /// <summary>Gets the completed response branch when it implements the requested contract.</summary>
    /// <typeparam name="TResponse">The response contract to match.</typeparam>
    /// <param name="result">Receives the matching response branch when available.</param>
    /// <returns><see langword="true"/> when a completed branch implements <typeparamref name="TResponse"/>; otherwise, <see langword="false"/>.</returns>
    public bool Is<TResponse>([NotNullWhen(true)] out Response<TResponse>? result)
        where TResponse : class
    {
        if (_response1 is Response<TResponse> response1)
        {
            result = response1;
            return true;
        }

        if (_response2 is Response<TResponse> response2)
        {
            result = response2;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>Deconstructs the response into its original branch tasks.</summary>
    /// <param name="response1Task">Receives the first response-contract task.</param>
    /// <param name="response2Task">Receives the second response-contract task.</param>
    public void Deconstruct(out Task<Response<TResponse1>> response1Task, out Task<Response<TResponse2>> response2Task)
    {
        _ = Context;
        response1Task = _response1Task!;
        response2Task = _response2Task!;
    }

    /// <summary>Creates a response wrapper from two response-contract tasks.</summary>
    /// <param name="source">The two response-contract tasks.</param>
    /// <returns>A wrapper over the successfully completed response branch.</returns>
    public static implicit operator Response<TResponse1, TResponse2>(
        (Task<Response<TResponse1>> response1, Task<Response<TResponse2>> response2) source)
    {
        return new Response<TResponse1, TResponse2>(source.response1, source.response2);
    }

    /// <summary>Gets the message id.</summary>
    public Guid? MessageId => Context.MessageId;

    /// <summary>Gets the request id.</summary>
    public Guid? RequestId => Context.RequestId;

    /// <summary>Gets the correlation id.</summary>
    public Guid? CorrelationId => Context.CorrelationId;

    /// <summary>Gets the conversation id.</summary>
    public Guid? ConversationId => Context.ConversationId;

    /// <summary>Gets the initiator id.</summary>
    public Guid? InitiatorId => Context.InitiatorId;

    /// <summary>Gets the expiration time.</summary>
    public DateTimeOffset? ExpirationTime => Context.ExpirationTime;

    /// <summary>Gets the source address.</summary>
    public Uri? SourceAddress => Context.SourceAddress;

    /// <summary>Gets the destination address.</summary>
    public Uri? DestinationAddress => Context.DestinationAddress;

    /// <summary>Gets the response address.</summary>
    public Uri? ResponseAddress => Context.ResponseAddress;

    /// <summary>Gets the fault address.</summary>
    public Uri? FaultAddress => Context.FaultAddress;

    /// <summary>Gets the sent time.</summary>
    public DateTimeOffset? SentTime => Context.SentTime;

    /// <summary>Gets the headers.</summary>
    public Headers Headers => Context.Headers;

    /// <summary>Gets the host.</summary>
    public HostInfo Host => Context.Host;

    /// <summary>Gets the message.</summary>
    public object Message => Context.Message;

    Response Context => _response
        ?? throw new InvalidOperationException("The response wrapper has not been constructed.");
}


/// <summary>
/// Represents the completed branch of a request that accepts three response contracts while preserving every branch task.
/// </summary>
/// <typeparam name="TResponse1">The first accepted response contract.</typeparam>
/// <typeparam name="TResponse2">The second accepted response contract.</typeparam>
/// <typeparam name="TResponse3">The third accepted response contract.</typeparam>
public readonly struct Response<TResponse1, TResponse2, TResponse3> :
    Response
    where TResponse1 : class
    where TResponse2 : class
    where TResponse3 : class
{
    readonly Response<TResponse1>? _response1;
    readonly Response<TResponse2>? _response2;
    readonly Response<TResponse3>? _response3;
    readonly Response? _response;
    readonly Task<Response<TResponse1>>? _response1Task;
    readonly Task<Response<TResponse2>>? _response2Task;
    readonly Task<Response<TResponse3>>? _response3Task;

    /// <summary>Creates a three-contract response from branch tasks when at least one branch completed successfully.</summary>
    /// <param name="response1">The first response-contract task.</param>
    /// <param name="response2">The second response-contract task.</param>
    /// <param name="response3">The third response-contract task.</param>
    public Response(Task<Response<TResponse1>> response1, Task<Response<TResponse2>> response2, Task<Response<TResponse3>> response3)
    {
        ArgumentNullException.ThrowIfNull(response1);
        ArgumentNullException.ThrowIfNull(response2);
        ArgumentNullException.ThrowIfNull(response3);

        _response1Task = response1;
        _response2Task = response2;
        _response3Task = response3;

        _response1 = response1.Status == TaskStatus.RanToCompletion ? response1.GetAwaiter().GetResult() : default;
        _response2 = response2.Status == TaskStatus.RanToCompletion ? response2.GetAwaiter().GetResult() : default;
        _response3 = response3.Status == TaskStatus.RanToCompletion ? response3.GetAwaiter().GetResult() : default;

        _response = _response1 as Response ?? _response2 as Response ?? _response3
            ?? throw new ArgumentException("At least one response must have completed");
    }

    /// <summary>Gets the first response branch when it completed successfully.</summary>
    /// <param name="result">Receives the first response branch when available.</param>
    /// <returns><see langword="true"/> when the first branch completed successfully; otherwise, <see langword="false"/>.</returns>
    public bool Is([NotNullWhen(true)] out Response<TResponse1>? result)
    {
        result = _response1;

        return result != default;
    }

    /// <summary>Gets the second response branch when it completed successfully.</summary>
    /// <param name="result">Receives the second response branch when available.</param>
    /// <returns><see langword="true"/> when the second branch completed successfully; otherwise, <see langword="false"/>.</returns>
    public bool Is([NotNullWhen(true)] out Response<TResponse2>? result)
    {
        result = _response2;

        return result != default;
    }

    /// <summary>Gets the third response branch when it completed successfully.</summary>
    /// <param name="result">Receives the third response branch when available.</param>
    /// <returns><see langword="true"/> when the third branch completed successfully; otherwise, <see langword="false"/>.</returns>
    public bool Is([NotNullWhen(true)] out Response<TResponse3>? result)
    {
        result = _response3;

        return result != default;
    }

    /// <summary>Gets the completed response branch when it implements the requested contract.</summary>
    /// <typeparam name="TResponse">The response contract to match.</typeparam>
    /// <param name="result">Receives the matching response branch when available.</param>
    /// <returns><see langword="true"/> when a completed branch implements <typeparamref name="TResponse"/>; otherwise, <see langword="false"/>.</returns>
    public bool Is<TResponse>([NotNullWhen(true)] out Response<TResponse>? result)
        where TResponse : class
    {
        if (_response1 is Response<TResponse> response1)
        {
            result = response1;
            return true;
        }

        if (_response2 is Response<TResponse> response2)
        {
            result = response2;
            return true;
        }

        if (_response3 is Response<TResponse> response3)
        {
            result = response3;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>Deconstructs the response into its original branch tasks.</summary>
    /// <param name="response1Task">Receives the first response-contract task.</param>
    /// <param name="response2Task">Receives the second response-contract task.</param>
    /// <param name="response3Task">Receives the third response-contract task.</param>
    public void Deconstruct(
        out Task<Response<TResponse1>> response1Task,
        out Task<Response<TResponse2>> response2Task,
        out Task<Response<TResponse3>> response3Task)
    {
        _ = Context;
        response1Task = _response1Task!;
        response2Task = _response2Task!;
        response3Task = _response3Task!;
    }

    /// <summary>Creates a response wrapper from three response-contract tasks.</summary>
    /// <param name="source">The three response-contract tasks.</param>
    /// <returns>A wrapper over the successfully completed response branch.</returns>
    public static implicit operator Response<TResponse1, TResponse2, TResponse3>(
        (Task<Response<TResponse1>> response1, Task<Response<TResponse2>> response2, Task<Response<TResponse3>> response3) source)
    {
        return new Response<TResponse1, TResponse2, TResponse3>(source.response1, source.response2, source.response3);
    }

    /// <summary>Gets the message id.</summary>
    public Guid? MessageId => Context.MessageId;

    /// <summary>Gets the request id.</summary>
    public Guid? RequestId => Context.RequestId;

    /// <summary>Gets the correlation id.</summary>
    public Guid? CorrelationId => Context.CorrelationId;

    /// <summary>Gets the conversation id.</summary>
    public Guid? ConversationId => Context.ConversationId;

    /// <summary>Gets the initiator id.</summary>
    public Guid? InitiatorId => Context.InitiatorId;

    /// <summary>Gets the expiration time.</summary>
    public DateTimeOffset? ExpirationTime => Context.ExpirationTime;

    /// <summary>Gets the source address.</summary>
    public Uri? SourceAddress => Context.SourceAddress;

    /// <summary>Gets the destination address.</summary>
    public Uri? DestinationAddress => Context.DestinationAddress;

    /// <summary>Gets the response address.</summary>
    public Uri? ResponseAddress => Context.ResponseAddress;

    /// <summary>Gets the fault address.</summary>
    public Uri? FaultAddress => Context.FaultAddress;

    /// <summary>Gets the sent time.</summary>
    public DateTimeOffset? SentTime => Context.SentTime;

    /// <summary>Gets the headers.</summary>
    public Headers Headers => Context.Headers;

    /// <summary>Gets the host.</summary>
    public HostInfo Host => Context.Host;

    /// <summary>Gets the message.</summary>
    public object Message => Context.Message;

    Response Context => _response
        ?? throw new InvalidOperationException("The response wrapper has not been constructed.");
}
