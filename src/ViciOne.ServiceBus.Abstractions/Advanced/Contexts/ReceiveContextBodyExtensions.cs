namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides access to the received message body.</summary>
public static class ReceiveContextBodyExtensions
{
    /// <summary>Opens the serialized message body as a stream.</summary>
    /// <param name="context">The receive context that owns the body.</param>
    /// <returns>A readable stream over the received body; the caller owns the returned stream.</returns>
    public static Stream OpenBodyStream(this ReceiveContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Body.OpenReadStream();
    }

    /// <summary>Copies the serialized message body into a new array.</summary>
    /// <param name="context">The receive context that owns the body.</param>
    /// <returns>An independently mutable copy of the complete body content.</returns>
    public static byte[] GetBodyContent(this ReceiveContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Body.ToArray();
    }
}
