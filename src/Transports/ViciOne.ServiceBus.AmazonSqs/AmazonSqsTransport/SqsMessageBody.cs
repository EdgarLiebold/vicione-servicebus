using System.Text.Json;
using Amazon.SQS.Model;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a sqs message body implementation.
/// </summary>
public class SqsMessageBody :
    StringMessageBody,
    JsonMessageBody
{
    readonly Message _message;
    JsonElement? _topicArn;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public SqsMessageBody(Message message)
        : base(message.Body)
    {
        _message = message;
    }

    /// <summary>
    /// Gets the topic arn value.
    /// </summary>
    public string? TopicArn => _topicArn?.GetString();

    /// <summary>
    /// Gets json element.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
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
