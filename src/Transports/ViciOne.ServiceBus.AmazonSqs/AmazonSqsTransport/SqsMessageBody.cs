using System.Text.Json;
using Amazon.SQS.Model;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Reads a native Amazon SQS body or unwraps the JSON envelope delivered by Amazon SNS.</summary>
public sealed class SqsMessageBody :
    StringMessageBody,
    IJsonMessageBody
{
    readonly Message _message;
    JsonElement? _topicArn;

    /// <summary>Initializes a body reader for an Amazon SQS message.</summary>
    /// <param name="message">The received Amazon SQS message.</param>
    public SqsMessageBody(Message message)
        : base(message.Body)
    {
        _message = message;
    }

    /// <summary>Gets the Amazon SNS topic ARN discovered while parsing an enveloped message.</summary>
    public string? TopicArn => _topicArn?.GetString();

    /// <summary>Parses the body and unwraps an Amazon SNS <c>Message</c> payload when an SNS topic ARN is present.</summary>
    /// <param name="options">The JSON serializer options used for both envelope and nested payload.</param>
    /// <returns>The native body element, the unwrapped SNS payload, or <see langword="null"/> when the body is absent.</returns>
    public JsonElement? GetJsonElement(JsonSerializerOptions options)
    {
        if (_message.Body == null)
            return null;

        var jsonElement = JsonSerializer.Deserialize<JsonElement>(_message.Body, options);

        if (jsonElement.TryGetProperty("TopicArn", out var topicElement) || jsonElement.TryGetProperty("topicArn", out topicElement))
        {
            _topicArn = topicElement;

            if (jsonElement.TryGetProperty("Message", out var messageElement) || jsonElement.TryGetProperty("message", out messageElement))
                return JsonSerializer.Deserialize<JsonElement>(messageElement.GetString() ?? string.Empty, options);
        }

        return jsonElement;
    }
}
