using System.IO;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides access to the received message body.
/// </summary>
public static class ReceiveContextBodyExtensions
{
    /// <summary>
    /// Opens the message body as a stream owned by the caller.
    /// </summary>
    /// <param name="context">The receive context that owns the body.</param>
    /// <returns>A readable stream over the received body.</returns>
    public static Stream GetBodyStream(this ReceiveContext context)
    {
        return context.Body.GetStream();
    }

    /// <summary>
    /// Copies the message body to a byte array.
    /// </summary>
    /// <param name="context">The receive context that owns the body.</param>
    /// <returns>The received body bytes.</returns>
    public static byte[] GetBody(this ReceiveContext context)
    {
        return context.Body.GetBytes();
    }
}
