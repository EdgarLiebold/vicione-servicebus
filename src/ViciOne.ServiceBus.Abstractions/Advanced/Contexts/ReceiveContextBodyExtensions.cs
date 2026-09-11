namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides access to the received message body.</summary>
public static class ReceiveContextBodyExtensions
{
    /// <summary>Opens the serialized message body as a stream.</summary>
    /// <param name="context">The receive context that owns the body.</param>
    /// <returns>A readable stream over the received body; the caller owns the returned stream.</returns>
    /// <exception cref="NotSupportedException">The receive source does not materialize serialized body content.</exception>
    public static Stream GetBodyStream(this ReceiveContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Body.GetStream();
    }

    /// <summary>Gets the serialized message body as bytes.</summary>
    /// <param name="context">The receive context that owns the body.</param>
    /// <returns>A byte array containing the complete body; ownership is defined by the body implementation.</returns>
    /// <exception cref="NotSupportedException">The receive source does not materialize serialized body content.</exception>
    public static byte[] GetBodyBytes(this ReceiveContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Body.GetBytes();
    }
}
