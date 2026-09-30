using System;
using System.Net.Mime;
using ViciOne.ServiceBus.Advanced;

namespace ViciOne.ServiceBus.Transports;

internal static class TransportBodyMaterializer
{
    private static readonly object MutationMarker = new();

    public static bool IsMutationFailure(Exception exception) =>
        exception.Data[MutationMarker] is true;

    public static byte[] ToArray<T>(SendContext<T> context) where T : class =>
        Materialize(context, static body => body.ToArray());

    public static string GetTransportText<T>(SendContext<T> context) where T : class =>
        Materialize(context, static body => body.GetRequiredTransportText());

    internal static TResult Read<T, TResult>(SendContext<T> context, Func<MessageBody, TResult> readBody)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(readBody);
        Guid? messageId = context.MessageId;
        string? contentType = context.ContentType?.ToString();
        TResult result;
        try
        {
            MessageBody body = (context as TransportSendContext)?.Body
                ?? throw new InvalidOperationException("A transport send context with a serialized body is required.");
            result = readBody(body);
        }
        catch (Exception failure)
        {
            if (context.MessageId != messageId
                || !string.Equals(context.ContentType?.ToString(), contentType, StringComparison.Ordinal))
            {
                RestoreMetadata(context, messageId, contentType);
                failure.Data[MutationMarker] = true;
            }

            throw;
        }

        bool messageIdChanged = context.MessageId != messageId;
        bool contentTypeChanged = !string.Equals(context.ContentType?.ToString(), contentType, StringComparison.Ordinal);
        if (messageIdChanged || contentTypeChanged)
        {
            RestoreMetadata(context, messageId, contentType);
            var failure = new MessageException(typeof(T), messageIdChanged
                ? "The SendContext MessageId changed during serialization"
                : "The SendContext ContentType changed during serialization");
            failure.Data[MutationMarker] = true;
            throw failure;
        }

        return result;
    }

    private static TResult Materialize<T, TResult>(SendContext<T> context, Func<MessageBody, TResult> readBody)
        where T : class => Read(context, readBody);

    private static void RestoreMetadata<T>(SendContext<T> context, Guid? messageId, string? contentType)
        where T : class
    {
        context.MessageId = messageId;
        context.ContentType = contentType is null ? null : new ContentType(contentType);
    }
}
