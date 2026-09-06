using System;
using System.Threading;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>The client factory is used to create request clients.</summary>
public interface IClientFactory
{
    /// <summary>Gets the context.</summary>
    ClientFactoryContext Context { get; }

    /// <summary>Create a request, using the message specified. If a destinationAddress for the message cannot be found, the message will be published.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    RequestHandle<T> CreateRequest<T>(T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Create a request, using the message specified. If a destinationAddress for the message cannot be found, the message will be published.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="destinationAddress">The destination service address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    RequestHandle<T> CreateRequest<T>(Uri destinationAddress, T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Create a request, using the message specified. If a destinationAddress for the message cannot be found, the message will be published.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="consumeContext">The consumeContext currently being processed.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Create a request, using the message specified. If a destinationAddress for the message cannot be found, the message will be published.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="consumeContext">The consumeContext currently being processed.</param>
    /// <param name="destinationAddress">The destination service address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, T message, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Create a request, using the message specified. If a destinationAddress for the message cannot be found, the message will be published.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="values">The values to initialize the message.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    RequestHandle<T> CreateRequest<T>(object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Create a request, using the message specified. If a destinationAddress for the message cannot be found, the message will be published.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="destinationAddress">The destination service address.</param>
    /// <param name="values">The values to initialize the message.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    RequestHandle<T> CreateRequest<T>(Uri destinationAddress, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Create a request, using the message specified. If a destinationAddress for the message cannot be found, the message will be published.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="consumeContext">The consumeContext currently being processed.</param>
    /// <param name="values">The values to initialize the message.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Create a request, using the message specified. If a destinationAddress for the message cannot be found, the message will be published.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="consumeContext">The consumeContext currently being processed.</param>
    /// <param name="destinationAddress">The destination service address.</param>
    /// <param name="values">The values to initialize the message.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created request.</returns>
    RequestHandle<T> CreateRequest<T>(ConsumeContext consumeContext, Uri destinationAddress, object values, RequestTimeout timeout = default, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Create a request client for the specified message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>The created request client.</returns>
    IRequestClient<T> CreateRequestClient<T>(RequestTimeout timeout = default)
        where T : class;

    /// <summary>Create a request client for the specified message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="consumeContext">The consumeContext currently being processed.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>The created request client.</returns>
    IRequestClient<T> CreateRequestClient<T>(ConsumeContext? consumeContext, RequestTimeout timeout = default)
        where T : class;

    /// <summary>Create a request client, using the specified service address.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination service address.</param>
    /// <param name="timeout">The default timeout for requests.</param>
    /// <returns>The created request client.</returns>
    IRequestClient<T> CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
        where T : class;

    /// <summary>Create a request client, using the specified service address.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consumeContext currently being processed.</param>
    /// <param name="destinationAddress">The destination service address.</param>
    /// <param name="timeout">The default timeout for requests.</param>
    /// <returns>The created request client.</returns>
    IRequestClient<T> CreateRequestClient<T>(ConsumeContext? consumeContext, Uri destinationAddress, RequestTimeout timeout = default)
        where T : class;
}
