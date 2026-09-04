using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// The base response type, which can be used to pattern match, via deconstruct, to the accepted
/// response types.
/// </summary>
public interface Response :
    MessageContext
{
    /// <summary>
    /// Gets the message value.
    /// </summary>
    object Message { get; }
}


/// <summary>
/// The response for a request with a single response type, or a request with multiple response types
/// that has been matched to a specific type.
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface Response<out TResponse> :
    Response
    where TResponse : class
{
    /// <summary>
    /// The response message that was received
    /// </summary>
    new TResponse Message { get; }
}


/// <summary>
/// The response for a request that accepts two response types, which can be matched easily or converted back into a tuple of
/// tasks.
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
public readonly struct Response<T1, T2> :
    Response
    where T1 : class
    where T2 : class
{
    readonly Response<T1>? _response1;
    readonly Response<T2>? _response2;
    readonly Response _response;
    readonly Task<Response<T1>> _response1Task;
    readonly Task<Response<T2>> _response2Task;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="response1">The response1 value.</param>
    /// <param name="response2">The response2 value.</param>
    public Response(Task<Response<T1>> response1, Task<Response<T2>> response2)
    {
        ArgumentNullException.ThrowIfNull(response1);
        ArgumentNullException.ThrowIfNull(response2);

        _response1Task = response1;
        _response2Task = response2;

        _response1 = response1.Status == TaskStatus.RanToCompletion ? response1.GetAwaiter().GetResult() : default;
        _response2 = response2.Status == TaskStatus.RanToCompletion ? response2.GetAwaiter().GetResult() : default;

        _response = _response1 as Response ?? _response2 ?? throw new ArgumentException("At least one response must have completed");
    }

    /// <summary>
    /// Performs the is operation.
    /// </summary>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Is([NotNullWhen(true)] out Response<T1>? result)
    {
        result = _response1;

        return result != default;
    }

    /// <summary>
    /// Performs the is operation.
    /// </summary>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Is([NotNullWhen(true)] out Response<T2>? result)
    {
        result = _response2;

        return result != default;
    }

    /// <summary>
    /// Performs the is operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Is<T>([NotNullWhen(true)] out Response<T>? result)
        where T : class
    {
        if (_response1 is Response<T> response1)
        {
            result = response1;
            return true;
        }

        if (_response2 is Response<T> response2)
        {
            result = response2;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Deconstructs this value into its components.
    /// </summary>
    /// <param name="r1">The r1 value.</param>
    /// <param name="r2">The r2 value.</param>
    public void Deconstruct(out Task<Response<T1>> r1, out Task<Response<T2>> r2)
    {
        r1 = _response1Task;
        r2 = _response2Task;
    }

    /// <summary>
    /// Converts a value to <see cref="Response&lt;T1, T2&gt;" />.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <returns>The result of the operation.</returns>
    public static implicit operator Response<T1, T2>((Task<Response<T1>> response1, Task<Response<T2>> response2) source)
    {
        return new Response<T1, T2>(source.response1, source.response2);
    }

    /// <summary>
    /// Gets the message id value.
    /// </summary>
    public Guid? MessageId => _response.MessageId;

    /// <summary>
    /// Gets the request id value.
    /// </summary>
    public Guid? RequestId => _response.RequestId;

    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    public Guid? CorrelationId => _response.CorrelationId;

    /// <summary>
    /// Gets the conversation id value.
    /// </summary>
    public Guid? ConversationId => _response.ConversationId;

    /// <summary>
    /// Gets the initiator id value.
    /// </summary>
    public Guid? InitiatorId => _response.InitiatorId;

    /// <summary>
    /// Gets the expiration time value.
    /// </summary>
    public DateTimeOffset? ExpirationTime => _response.ExpirationTime;

    /// <summary>
    /// Gets the source address value.
    /// </summary>
    public Uri? SourceAddress => _response.SourceAddress;

    /// <summary>
    /// Gets the destination address value.
    /// </summary>
    public Uri? DestinationAddress => _response.DestinationAddress;

    /// <summary>
    /// Gets the response address value.
    /// </summary>
    public Uri? ResponseAddress => _response.ResponseAddress;

    /// <summary>
    /// Gets the fault address value.
    /// </summary>
    public Uri? FaultAddress => _response.FaultAddress;

    /// <summary>
    /// Gets the sent time value.
    /// </summary>
    public DateTimeOffset? SentTime => _response.SentTime;

    /// <summary>
    /// Gets the headers value.
    /// </summary>
    public Headers Headers => _response.Headers;

    /// <summary>
    /// Gets the host value.
    /// </summary>
    public HostInfo Host => _response.Host;

    /// <summary>
    /// Gets the message value.
    /// </summary>
    public object Message => _response.Message;
}


/// <summary>
/// The response for a request that accepts two response types, which can be matched easily or converted back into a tuple of
/// tasks.
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
/// <typeparam name="T3"></typeparam>
public readonly struct Response<T1, T2, T3> :
    Response
    where T1 : class
    where T2 : class
    where T3 : class
{
    readonly Response<T1>? _response1;
    readonly Response<T2>? _response2;
    readonly Response<T3>? _response3;
    readonly Response _response;
    readonly Task<Response<T1>> _response1Task;
    readonly Task<Response<T2>> _response2Task;
    readonly Task<Response<T3>> _response3Task;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="response1">The response1 value.</param>
    /// <param name="response2">The response2 value.</param>
    /// <param name="response3">The response3 value.</param>
    public Response(Task<Response<T1>> response1, Task<Response<T2>> response2, Task<Response<T3>> response3)
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

    /// <summary>
    /// Performs the is operation.
    /// </summary>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Is([NotNullWhen(true)] out Response<T1>? result)
    {
        result = _response1;

        return result != default;
    }

    /// <summary>
    /// Performs the is operation.
    /// </summary>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Is([NotNullWhen(true)] out Response<T2>? result)
    {
        result = _response2;

        return result != default;
    }

    /// <summary>
    /// Performs the is operation.
    /// </summary>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Is([NotNullWhen(true)] out Response<T3>? result)
    {
        result = _response3;

        return result != default;
    }

    /// <summary>
    /// Performs the is operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Is<T>([NotNullWhen(true)] out Response<T>? result)
        where T : class
    {
        if (_response1 is Response<T> response1)
        {
            result = response1;
            return true;
        }

        if (_response2 is Response<T> response2)
        {
            result = response2;
            return true;
        }

        if (_response3 is Response<T> response3)
        {
            result = response3;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Deconstructs this value into its components.
    /// </summary>
    /// <param name="r1">The r1 value.</param>
    /// <param name="r2">The r2 value.</param>
    /// <param name="r3">The r3 value.</param>
    public void Deconstruct(out Task<Response<T1>> r1, out Task<Response<T2>> r2, out Task<Response<T3>> r3)
    {
        r1 = _response1Task;
        r2 = _response2Task;
        r3 = _response3Task;
    }

    /// <summary>
    /// Converts a value to <see cref="Response&lt;T1, T2, T3&gt;" />.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <returns>The result of the operation.</returns>
    public static implicit operator Response<T1, T2, T3>((Task<Response<T1>> response1, Task<Response<T2>> response2, Task<Response<T3>> response3) source)
    {
        return new Response<T1, T2, T3>(source.response1, source.response2, source.response3);
    }

    /// <summary>
    /// Gets the message id value.
    /// </summary>
    public Guid? MessageId => _response.MessageId;

    /// <summary>
    /// Gets the request id value.
    /// </summary>
    public Guid? RequestId => _response.RequestId;

    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    public Guid? CorrelationId => _response.CorrelationId;

    /// <summary>
    /// Gets the conversation id value.
    /// </summary>
    public Guid? ConversationId => _response.ConversationId;

    /// <summary>
    /// Gets the initiator id value.
    /// </summary>
    public Guid? InitiatorId => _response.InitiatorId;

    /// <summary>
    /// Gets the expiration time value.
    /// </summary>
    public DateTimeOffset? ExpirationTime => _response.ExpirationTime;

    /// <summary>
    /// Gets the source address value.
    /// </summary>
    public Uri? SourceAddress => _response.SourceAddress;

    /// <summary>
    /// Gets the destination address value.
    /// </summary>
    public Uri? DestinationAddress => _response.DestinationAddress;

    /// <summary>
    /// Gets the response address value.
    /// </summary>
    public Uri? ResponseAddress => _response.ResponseAddress;

    /// <summary>
    /// Gets the fault address value.
    /// </summary>
    public Uri? FaultAddress => _response.FaultAddress;

    /// <summary>
    /// Gets the sent time value.
    /// </summary>
    public DateTimeOffset? SentTime => _response.SentTime;

    /// <summary>
    /// Gets the headers value.
    /// </summary>
    public Headers Headers => _response.Headers;

    /// <summary>
    /// Gets the host value.
    /// </summary>
    public HostInfo Host => _response.Host;

    /// <summary>
    /// Gets the message value.
    /// </summary>
    public object Message => _response.Message;
}
