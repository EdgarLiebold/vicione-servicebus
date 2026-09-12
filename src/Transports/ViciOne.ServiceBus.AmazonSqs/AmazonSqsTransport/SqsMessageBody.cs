using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Amazon.SQS.Model;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Owns a native Amazon SQS body and exposes the application payload carried directly or through Amazon SNS.</summary>
internal sealed class SqsMessageBody :
    MessageBody,
    IJsonMessageBody,
    TransportTextMessageBody
{
    readonly bool _hasBody;
    readonly Lazy<NotificationBody> _payload;
    readonly string _text;
    readonly long _length;

    /// <summary>Creates an owned body snapshot from an Amazon SQS message.</summary>
    /// <param name="message">The received Amazon SQS message to snapshot.</param>
    /// <param name="requiresSnsNotificationEnvelope">Whether the native body must be an Amazon SNS notification envelope.</param>
    public SqsMessageBody(Message message, bool requiresSnsNotificationEnvelope = false)
    {
        ArgumentNullException.ThrowIfNull(message);
        _hasBody = message.Body is not null;
        _text = message.Body ?? string.Empty;
        _length = MessageDefaults.Encoding.GetByteCount(_text);
        _payload = new Lazy<NotificationBody>(() => requiresSnsNotificationEnvelope
            ? ParseNotification(_text)
            : NotificationBody.Direct(_text));
    }

    /// <summary>Gets the native UTF-8 snapshot length used for admission.</summary>
    public long Length => _length;

    /// <summary>Copies the exact native SQS body as UTF-8 into a new array.</summary>
    /// <returns>An independently mutable copy of the native body.</returns>
    public byte[] ToArray() => MessageDefaults.Encoding.GetBytes(_text);

    /// <summary>Opens a new read-only stream over the body snapshot.</summary>
    /// <returns>An independently disposable stream positioned at the beginning of the body.</returns>
    public Stream OpenReadStream() => new MemoryStream(ToArray(), false);

    /// <summary>Tries to get the exact native SQS body text.</summary>
    /// <param name="text">The native text snapshot, including an empty body.</param>
    /// <returns><see langword="true" /> when the native message has a body; otherwise, <see langword="false" />.</returns>
    public bool TryGetTransportText([NotNullWhen(true)] out string? text)
    {
        text = _hasBody ? _text : null;
        return text is not null;
    }

    /// <summary>Gets the Amazon SNS topic ARN discovered in a notification envelope.</summary>
    public string? TopicArn => _hasBody ? _payload.Value.TopicArn : null;

    /// <summary>Parses the extracted application payload as JSON.</summary>
    /// <param name="options">The JSON serializer options used for the application payload.</param>
    /// <returns>The application payload, or <see langword="null"/> when the native body is absent.</returns>
    public JsonElement? GetJsonElement(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!_hasBody)
            return null;

        return JsonSerializer.Deserialize<JsonElement>(_payload.Value.PayloadText, options);
    }

    /// <summary>Tries to expose the direct SQS payload or the message extracted from an SNS notification.</summary>
    /// <param name="text">The serializer text when the native message has a body.</param>
    /// <returns><see langword="true" /> when the native message has a body; otherwise, <see langword="false" />.</returns>
    public bool TryGetPayloadText([NotNullWhen(true)] out string? text)
    {
        text = _hasBody ? _payload.Value.PayloadText : null;
        return text is not null;
    }

    /// <summary>Tries to read a string message attribute carried by an Amazon SNS notification envelope.</summary>
    /// <param name="key">The case-sensitive Amazon SNS message-attribute name.</param>
    /// <param name="value">The attribute value when it is present.</param>
    /// <returns><see langword="true" /> when the envelope contains the requested string attribute; otherwise, <see langword="false" />.</returns>
    internal bool TryGetNotificationHeader(string key, [NotNullWhen(true)] out string? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_hasBody && _payload.Value.MessageAttributes.TryGetValue(key, out value))
            return true;

        value = null;
        return false;
    }

    /// <summary>Enumerates string message attributes carried by an Amazon SNS notification envelope.</summary>
    /// <returns>The snapshot of notification attributes, or an empty sequence when the body is direct SQS content.</returns>
    internal IEnumerable<KeyValuePair<string, string>> GetNotificationHeaders()
    {
        return _hasBody
            ? _payload.Value.MessageAttributes
            : [];
    }

    static NotificationBody ParseNotification(string text)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The Amazon SQS body is not a structurally valid Amazon SNS notification envelope.", exception);
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !TryGetString(root, "Type", out var type)
                || !string.Equals(type, "Notification", StringComparison.Ordinal)
                || !TryGetString(root, "MessageId", out var messageId)
                || !Guid.TryParseExact(messageId, "D", out _)
                || !TryGetString(root, "TopicArn", out var topicArn)
                || !IsSnsTopicArn(topicArn)
                || !TryGetString(root, "Message", out var payload)
                || !TryGetString(root, "Timestamp", out var timestamp)
                || !DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out _)
                || !TryGetString(root, "SignatureVersion", out var signatureVersion)
                || signatureVersion is not ("1" or "2")
                || !TryGetString(root, "Signature", out var signature)
                || string.IsNullOrWhiteSpace(signature)
                || !TryGetString(root, "SigningCertURL", out var signingCertificateUrl)
                || !Uri.TryCreate(signingCertificateUrl, UriKind.Absolute, out var signingCertificate)
                || signingCertificate.Scheme is not ("https" or "http"))
                throw new InvalidDataException("The Amazon SQS body is not a structurally valid Amazon SNS notification envelope.");

            return new NotificationBody(payload, topicArn, ReadMessageAttributes(root));
        }
    }

    static IReadOnlyDictionary<string, string> ReadMessageAttributes(JsonElement root)
    {
        if (!TryGetProperty(root, "MessageAttributes", out var attributes))
            return NotificationBody.EmptyMessageAttributes;

        if (attributes.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("An Amazon SNS notification MessageAttributes value must be a JSON object.");

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (JsonProperty attribute in attributes.EnumerateObject())
        {
            if (attribute.Value.ValueKind != JsonValueKind.Object
                || !TryGetString(attribute.Value, "Type", out _)
                || !TryGetString(attribute.Value, "Value", out var value))
                throw new InvalidDataException($"Amazon SNS notification attribute '{attribute.Name}' must contain string Type and Value properties.");

            if (!result.TryAdd(attribute.Name, value))
                throw new InvalidDataException($"Amazon SNS notification contains duplicate attribute '{attribute.Name}'.");
        }

        return result;
    }

    static bool TryGetString(JsonElement element, string name, [NotNullWhen(true)] out string? value)
    {
        if (TryGetProperty(element, name, out var property) && property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString();
            return value is not null;
        }

        value = null;
        return false;
    }

    static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!property.Name.Equals(name, StringComparison.Ordinal))
                continue;

            value = property.Value;
            return true;
        }

        value = default;
        return false;
    }

    static bool IsSnsTopicArn(string value)
    {
        string[] parts = value.Split(':', 6, StringSplitOptions.None);
        return parts.Length == 6
            && string.Equals(parts[0], "arn", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(parts[1])
            && string.Equals(parts[2], "sns", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(parts[3])
            && !string.IsNullOrWhiteSpace(parts[4])
            && !string.IsNullOrWhiteSpace(parts[5]);
    }

    sealed record NotificationBody(string PayloadText, string? TopicArn, IReadOnlyDictionary<string, string> MessageAttributes)
    {
        internal static readonly IReadOnlyDictionary<string, string> EmptyMessageAttributes =
            new Dictionary<string, string>(StringComparer.Ordinal);

        internal static NotificationBody Direct(string payloadText) => new(payloadText, null, EmptyMessageAttributes);
    }
}
