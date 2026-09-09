using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides response deconstruction and response-contract acceptance checks.</summary>
public static class ResponseExtensions
{
    /// <summary>Deconstructs a response into its message context and message for pattern matching.</summary>
    /// <param name="response">The response to deconstruct.</param>
    /// <param name="context">Receives the complete response context.</param>
    /// <param name="message">Receives the response message.</param>
    public static void Deconstruct(this Response response, out Response context, out object message)
    {
        ArgumentNullException.ThrowIfNull(response);
        context = response;
        message = response.Message;
    }

    /// <summary>Determines whether a request explicitly declares a response contract as accepted.</summary>
    /// <typeparam name="T">The response contract to inspect.</typeparam>
    /// <param name="context">The consumed request context.</param>
    /// <returns><see langword="true"/> when the request has a response address and declares the exact response contract; otherwise, <see langword="false"/>.</returns>
    public static bool IsResponseAccepted<T>(this ConsumeContext context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.ResponseAddress == null)
            return false;

        var acceptTypes = context.GetHeader<IList<string>>(MessageHeaders.Request.Accept);
        if (acceptTypes == null || acceptTypes.Count <= 0)
            return false;

        var matchingTypeNames = MessageTypeCache<T>.MessageTypeNames;

        return acceptTypes.Any(accept => matchingTypeNames.Any(typeName => typeName.Equals(accept, StringComparison.Ordinal)));
    }
}
