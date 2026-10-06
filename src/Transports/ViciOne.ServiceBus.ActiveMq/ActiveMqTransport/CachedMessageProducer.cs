using System;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Caching;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Decorates an Apache NMS producer and reports cache usage from send, message-creation, and asynchronous-close methods.</summary>
public class CachedMessageProducer :
    IMessageProducer,
    IResourceUsageSource
{
    readonly IMessageProducer _producer;

    /// <summary>Creates a cache-aware wrapper for a native message producer.</summary>
    /// <param name="destination">The destination associated with the cached producer.</param>
    /// <param name="producer">The native producer to wrap.</param>
    public CachedMessageProducer(IDestination destination, IMessageProducer producer)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(producer);

        Destination = destination;
        _producer = producer;
    }

    /// <summary>Gets the destination associated with this cached producer.</summary>
    public IDestination Destination { get; }

    /// <summary>Disposes the wrapped producer.</summary>
    public void Dispose()
    {
        _producer.Dispose();
    }

    /// <summary>Reports cache usage and sends a message to the producer's default destination.</summary>
    /// <param name="message">The native message to send.</param>
    public void Send(IMessage message)
    {
        Used?.Invoke();
        _producer.Send(message);
    }

    /// <summary>Reports cache usage and sends a message with explicit delivery settings.</summary>
    /// <param name="message">The native message to send.</param>
    /// <param name="deliveryMode">Whether delivery is persistent or non-persistent.</param>
    /// <param name="priority">The Apache NMS message priority.</param>
    /// <param name="timeToLive">The interval after which the broker may expire the message.</param>
    public void Send(IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
    {
        Used?.Invoke();
        _producer.Send(message, deliveryMode, priority, timeToLive);
    }

    /// <summary>Reports cache usage and sends a message to an explicit destination.</summary>
    /// <param name="destination">The native destination.</param>
    /// <param name="message">The native message to send.</param>
    public void Send(IDestination destination, IMessage message)
    {
        Used?.Invoke();
        _producer.Send(destination, message);
    }

    /// <summary>Reports cache usage and sends a message to an explicit destination with explicit delivery settings.</summary>
    /// <param name="destination">The native destination.</param>
    /// <param name="message">The native message to send.</param>
    /// <param name="deliveryMode">Whether delivery is persistent or non-persistent.</param>
    /// <param name="priority">The Apache NMS message priority.</param>
    /// <param name="timeToLive">The interval after which the broker may expire the message.</param>
    public void Send(IDestination destination, IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
    {
        Used?.Invoke();
        _producer.Send(destination, message, deliveryMode, priority, timeToLive);
    }

    /// <summary>Closes the wrapped native producer.</summary>
    public void Close()
    {
        _producer.Close();
    }

    /// <summary>Reports cache usage and creates a native message without a typed body.</summary>
    /// <returns>The created Apache NMS message.</returns>
    public IMessage CreateMessage()
    {
        Used?.Invoke();
        return _producer.CreateMessage();
    }

    /// <summary>Reports cache usage and creates an empty native text message.</summary>
    /// <returns>The created Apache NMS text message.</returns>
    public ITextMessage CreateTextMessage()
    {
        Used?.Invoke();
        return _producer.CreateTextMessage();
    }

    /// <summary>Reports cache usage and creates a native text message.</summary>
    /// <param name="text">The message body text.</param>
    /// <returns>The created Apache NMS text message.</returns>
    public ITextMessage CreateTextMessage(string text)
    {
        Used?.Invoke();
        return _producer.CreateTextMessage(text);
    }

    /// <summary>Reports cache usage and creates a native map message.</summary>
    /// <returns>The created Apache NMS map message.</returns>
    public IMapMessage CreateMapMessage()
    {
        Used?.Invoke();
        return _producer.CreateMapMessage();
    }

    /// <summary>Reports cache usage and creates a native object message.</summary>
    /// <param name="body">The object message body.</param>
    /// <returns>The created Apache NMS object message.</returns>
    public IObjectMessage CreateObjectMessage(object body)
    {
        Used?.Invoke();
        return _producer.CreateObjectMessage(body);
    }

    /// <summary>Reports cache usage and creates an empty native byte message.</summary>
    /// <returns>The created Apache NMS byte message.</returns>
    public IBytesMessage CreateBytesMessage()
    {
        Used?.Invoke();
        return _producer.CreateBytesMessage();
    }

    /// <summary>Reports cache usage and creates a native byte message.</summary>
    /// <param name="body">The message body bytes.</param>
    /// <returns>The created Apache NMS byte message.</returns>
    public IBytesMessage CreateBytesMessage(byte[] body)
    {
        Used?.Invoke();
        return _producer.CreateBytesMessage(body);
    }

    /// <summary>Reports cache usage and creates a native stream message.</summary>
    /// <returns>The created Apache NMS stream message.</returns>
    public IStreamMessage CreateStreamMessage()
    {
        Used?.Invoke();
        return _producer.CreateStreamMessage();
    }

    /// <summary>Reports cache usage and asynchronously sends a message to the producer's default destination.</summary>
    /// <param name="message">The native message to send.</param>
    /// <returns>A task that completes when the native send completes.</returns>
    public Task SendAsync(IMessage message)
    {
        Used?.Invoke();
        return _producer.SendAsync(message);
    }

    /// <summary>Reports cache usage and asynchronously sends a message with explicit delivery settings.</summary>
    /// <param name="message">The native message to send.</param>
    /// <param name="deliveryMode">Whether delivery is persistent or non-persistent.</param>
    /// <param name="priority">The Apache NMS message priority.</param>
    /// <param name="timeToLive">The interval after which the broker may expire the message.</param>
    /// <returns>A task that completes when the native send completes.</returns>
    public Task SendAsync(IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
    {
        Used?.Invoke();
        return _producer.SendAsync(message, deliveryMode, priority, timeToLive);
    }

    /// <summary>Reports cache usage and asynchronously sends a message to an explicit destination.</summary>
    /// <param name="destination">The native destination.</param>
    /// <param name="message">The native message to send.</param>
    /// <returns>A task that completes when the native send completes.</returns>
    public Task SendAsync(IDestination destination, IMessage message)
    {
        Used?.Invoke();
        return _producer.SendAsync(destination, message);
    }

    /// <summary>Reports cache usage and asynchronously sends a message to an explicit destination with explicit delivery settings.</summary>
    /// <param name="destination">The native destination.</param>
    /// <param name="message">The native message to send.</param>
    /// <param name="deliveryMode">Whether delivery is persistent or non-persistent.</param>
    /// <param name="priority">The Apache NMS message priority.</param>
    /// <param name="timeToLive">The interval after which the broker may expire the message.</param>
    /// <returns>A task that completes when the native send completes.</returns>
    public Task SendAsync(IDestination destination, IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
    {
        Used?.Invoke();
        return _producer.SendAsync(destination, message, deliveryMode, priority, timeToLive);
    }

    /// <summary>Reports cache usage and asynchronously closes the wrapped native producer.</summary>
    /// <returns>A task that completes when the producer is closed.</returns>
    public Task CloseAsync()
    {
        Used?.Invoke();
        return _producer.CloseAsync();
    }

    /// <summary>Reports cache usage and asynchronously creates a native message without a typed body.</summary>
    /// <returns>A task that produces the created Apache NMS message.</returns>
    public Task<IMessage> CreateMessageAsync()
    {
        Used?.Invoke();
        return _producer.CreateMessageAsync();
    }

    /// <summary>Reports cache usage and asynchronously creates an empty native text message.</summary>
    /// <returns>A task that produces the created Apache NMS text message.</returns>
    public Task<ITextMessage> CreateTextMessageAsync()
    {
        Used?.Invoke();
        return _producer.CreateTextMessageAsync();
    }

    /// <summary>Reports cache usage and asynchronously creates a native text message.</summary>
    /// <param name="text">The message body text.</param>
    /// <returns>A task that produces the created Apache NMS text message.</returns>
    public Task<ITextMessage> CreateTextMessageAsync(string text)
    {
        Used?.Invoke();
        return _producer.CreateTextMessageAsync(text);
    }

    /// <summary>Reports cache usage and asynchronously creates a native map message.</summary>
    /// <returns>A task that produces the created Apache NMS map message.</returns>
    public Task<IMapMessage> CreateMapMessageAsync()
    {
        Used?.Invoke();
        return _producer.CreateMapMessageAsync();
    }

    /// <summary>Reports cache usage and asynchronously creates a native object message.</summary>
    /// <param name="body">The object message body.</param>
    /// <returns>A task that produces the created Apache NMS object message.</returns>
    public Task<IObjectMessage> CreateObjectMessageAsync(object body)
    {
        Used?.Invoke();
        return _producer.CreateObjectMessageAsync(body);
    }

    /// <summary>Reports cache usage and asynchronously creates an empty native byte message.</summary>
    /// <returns>A task that produces the created Apache NMS byte message.</returns>
    public Task<IBytesMessage> CreateBytesMessageAsync()
    {
        Used?.Invoke();
        return _producer.CreateBytesMessageAsync();
    }

    /// <summary>Reports cache usage and asynchronously creates a native byte message.</summary>
    /// <param name="body">The message body bytes.</param>
    /// <returns>A task that produces the created Apache NMS byte message.</returns>
    public Task<IBytesMessage> CreateBytesMessageAsync(byte[] body)
    {
        Used?.Invoke();
        return _producer.CreateBytesMessageAsync(body);
    }

    /// <summary>Reports cache usage and asynchronously creates a native stream message.</summary>
    /// <returns>A task that produces the created Apache NMS stream message.</returns>
    public Task<IStreamMessage> CreateStreamMessageAsync()
    {
        Used?.Invoke();
        return _producer.CreateStreamMessageAsync();
    }

    /// <summary>Gets or sets the wrapped producer's native message transformer.</summary>
    public ProducerTransformerDelegate ProducerTransformer
    {
        get => _producer.ProducerTransformer;
        set => _producer.ProducerTransformer = value;
    }

    /// <summary>Gets or sets the wrapped producer's default delivery mode.</summary>
    public MsgDeliveryMode DeliveryMode
    {
        get => _producer.DeliveryMode;
        set => _producer.DeliveryMode = value;
    }

    /// <summary>Gets or sets the wrapped producer's default message lifetime.</summary>
    public TimeSpan TimeToLive
    {
        get => _producer.TimeToLive;
        set => _producer.TimeToLive = value;
    }

    /// <summary>Gets or sets the wrapped producer's request timeout.</summary>
    public TimeSpan RequestTimeout
    {
        get => _producer.RequestTimeout;
        set => _producer.RequestTimeout = value;
    }

    /// <summary>Gets or sets the wrapped producer's default message priority.</summary>
    public MsgPriority Priority
    {
        get => _producer.Priority;
        set => _producer.Priority = value;
    }

    /// <summary>Gets or sets whether the provider suppresses broker message identifiers.</summary>
    public bool DisableMessageID
    {
        get => _producer.DisableMessageID;
        set => _producer.DisableMessageID = value;
    }

    /// <summary>Gets or sets whether the provider suppresses broker message timestamps.</summary>
    public bool DisableMessageTimestamp
    {
        get => _producer.DisableMessageTimestamp;
        set => _producer.DisableMessageTimestamp = value;
    }

    /// <summary>Gets or sets the wrapped producer's default delivery delay.</summary>
    public TimeSpan DeliveryDelay
    {
        get => _producer.DeliveryDelay;
        set => _producer.DeliveryDelay = value;
    }

    /// <summary>Occurs when a send, message-creation, or asynchronous-close method reports use of the wrapped producer.</summary>
    public event Action? Used;
}
