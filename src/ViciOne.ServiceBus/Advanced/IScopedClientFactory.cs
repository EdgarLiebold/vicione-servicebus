using System;
using System.Threading;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates request handles and clients within the current dependency-injection scope.</summary>
public interface IScopedClientFactory
{
    /// <summary>Creates a request that uses the configured message destination or publishes when no destination is configured.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    RequestHandle<T> CreateRequest<T>(T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Creates a request for an explicit destination.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="destinationAddress">The destination service address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    RequestHandle<T> CreateRequest<T>(Uri destinationAddress, T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Creates and initializes a request that uses the configured message destination or publishes when none is configured.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="values">The values to initialize the message.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    RequestHandle<T> CreateRequest<T>(object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Creates and initializes a request for an explicit destination.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="destinationAddress">The destination service address.</param>
    /// <param name="values">The values to initialize the message.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    RequestHandle<T> CreateRequest<T>(Uri destinationAddress, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Creates a request client for the specified message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>The created request client.</returns>
    IRequestClient<T> CreateRequestClient<T>(RequestTimeout timeout = default)
        where T : class;

    /// <summary>Creates a request client for an explicit service address.</summary>
    /// <typeparam name="T">The request message type.</typeparam>
    /// <param name="destinationAddress">The destination service address.</param>
    /// <param name="timeout">The default timeout for requests.</param>
    /// <returns>The created request client.</returns>
    IRequestClient<T> CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class;
}
