using System;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Caching;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides a cached message producer implementation.
/// </summary>
public class CachedMessageProducer :
    IMessageProducer,
    IResourceUsageSource
{
    readonly IMessageProducer _producer;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="destination">The destination value.</param>
    /// <param name="producer">The producer value.</param>
    public CachedMessageProducer(IDestination destination, IMessageProducer producer)
    {
        Destination = destination;
        _producer = producer;
    }

    /// <summary>
    /// Gets the destination value.
    /// </summary>
    public IDestination Destination { get; }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        _producer.Dispose();
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    public void Send(IMessage message)
    {
        Used?.Invoke();
        _producer.Send(message);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="deliveryMode">The delivery mode value.</param>
    /// <param name="priority">The priority value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    public void Send(IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
    {
        Used?.Invoke();
        _producer.Send(message, deliveryMode, priority, timeToLive);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="destination">The destination value.</param>
    /// <param name="message">The message value.</param>
    public void Send(IDestination destination, IMessage message)
    {
        Used?.Invoke();
        _producer.Send(destination, message);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="destination">The destination value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="deliveryMode">The delivery mode value.</param>
    /// <param name="priority">The priority value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    public void Send(IDestination destination, IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
    {
        Used?.Invoke();
        _producer.Send(destination, message, deliveryMode, priority, timeToLive);
    }

    /// <summary>
    /// Performs the close operation.
    /// </summary>
    public void Close()
    {
        _producer.Close();
    }

    /// <summary>
    /// Creates message.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IMessage CreateMessage()
    {
        Used?.Invoke();
        return _producer.CreateMessage();
    }

    /// <summary>
    /// Creates text message.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ITextMessage CreateTextMessage()
    {
        Used?.Invoke();
        return _producer.CreateTextMessage();
    }

    /// <summary>
    /// Creates text message.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <returns>The result of the operation.</returns>
    public ITextMessage CreateTextMessage(string text)
    {
        Used?.Invoke();
        return _producer.CreateTextMessage(text);
    }

    /// <summary>
    /// Creates map message.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IMapMessage CreateMapMessage()
    {
        Used?.Invoke();
        return _producer.CreateMapMessage();
    }

    /// <summary>
    /// Creates object message.
    /// </summary>
    /// <param name="body">The body value.</param>
    /// <returns>The result of the operation.</returns>
    public IObjectMessage CreateObjectMessage(object body)
    {
        Used?.Invoke();
        return _producer.CreateObjectMessage(body);
    }

    /// <summary>
    /// Creates bytes message.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IBytesMessage CreateBytesMessage()
    {
        Used?.Invoke();
        return _producer.CreateBytesMessage();
    }

    /// <summary>
    /// Creates bytes message.
    /// </summary>
    /// <param name="body">The body value.</param>
    /// <returns>The result of the operation.</returns>
    public IBytesMessage CreateBytesMessage(byte[] body)
    {
        Used?.Invoke();
        return _producer.CreateBytesMessage(body);
    }

    /// <summary>
    /// Creates stream message.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IStreamMessage CreateStreamMessage()
    {
        Used?.Invoke();
        return _producer.CreateStreamMessage();
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(IMessage message)
    {
        Used?.Invoke();
        return _producer.SendAsync(message);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="deliveryMode">The delivery mode value.</param>
    /// <param name="priority">The priority value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
    {
        Used?.Invoke();
        return _producer.SendAsync(message, deliveryMode, priority, timeToLive);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="destination">The destination value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(IDestination destination, IMessage message)
    {
        Used?.Invoke();
        return _producer.SendAsync(destination, message);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="destination">The destination value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="deliveryMode">The delivery mode value.</param>
    /// <param name="priority">The priority value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(IDestination destination, IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
    {
        Used?.Invoke();
        return _producer.SendAsync(destination, message, deliveryMode, priority, timeToLive);
    }

    /// <summary>
    /// Performs the close operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Task CloseAsync()
    {
        Used?.Invoke();
        return _producer.CloseAsync();
    }

    /// <summary>
    /// Creates message.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Task<IMessage> CreateMessageAsync()
    {
        Used?.Invoke();
        return _producer.CreateMessageAsync();
    }

    /// <summary>
    /// Creates text message.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Task<ITextMessage> CreateTextMessageAsync()
    {
        Used?.Invoke();
        return _producer.CreateTextMessageAsync();
    }

    /// <summary>
    /// Creates text message.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ITextMessage> CreateTextMessageAsync(string text)
    {
        Used?.Invoke();
        return _producer.CreateTextMessageAsync(text);
    }

    /// <summary>
    /// Creates map message.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Task<IMapMessage> CreateMapMessageAsync()
    {
        Used?.Invoke();
        return _producer.CreateMapMessageAsync();
    }

    /// <summary>
    /// Creates object message.
    /// </summary>
    /// <param name="body">The body value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IObjectMessage> CreateObjectMessageAsync(object body)
    {
        Used?.Invoke();
        return _producer.CreateObjectMessageAsync(body);
    }

    /// <summary>
    /// Creates bytes message.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Task<IBytesMessage> CreateBytesMessageAsync()
    {
        Used?.Invoke();
        return _producer.CreateBytesMessageAsync();
    }

    /// <summary>
    /// Creates bytes message.
    /// </summary>
    /// <param name="body">The body value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IBytesMessage> CreateBytesMessageAsync(byte[] body)
    {
        Used?.Invoke();
        return _producer.CreateBytesMessageAsync(body);
    }

    /// <summary>
    /// Creates stream message.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Task<IStreamMessage> CreateStreamMessageAsync()
    {
        Used?.Invoke();
        return _producer.CreateStreamMessageAsync();
    }

    /// <summary>
    /// Gets or sets the producer transformer value.
    /// </summary>
    public ProducerTransformerDelegate ProducerTransformer
    {
        get => _producer.ProducerTransformer;
        set => _producer.ProducerTransformer = value;
    }

    /// <summary>
    /// Gets or sets the delivery mode value.
    /// </summary>
    public MsgDeliveryMode DeliveryMode
    {
        get => _producer.DeliveryMode;
        set => _producer.DeliveryMode = value;
    }

    /// <summary>
    /// Gets or sets the time to live value.
    /// </summary>
    public TimeSpan TimeToLive
    {
        get => _producer.TimeToLive;
        set => _producer.TimeToLive = value;
    }

    /// <summary>
    /// Gets or sets the request timeout value.
    /// </summary>
    public TimeSpan RequestTimeout
    {
        get => _producer.RequestTimeout;
        set => _producer.RequestTimeout = value;
    }

    /// <summary>
    /// Gets or sets the priority value.
    /// </summary>
    public MsgPriority Priority
    {
        get => _producer.Priority;
        set => _producer.Priority = value;
    }

    /// <summary>
    /// Gets or sets the disable message id value.
    /// </summary>
    public bool DisableMessageID
    {
        get => _producer.DisableMessageID;
        set => _producer.DisableMessageID = value;
    }

    /// <summary>
    /// Gets or sets the disable message timestamp value.
    /// </summary>
    public bool DisableMessageTimestamp
    {
        get => _producer.DisableMessageTimestamp;
        set => _producer.DisableMessageTimestamp = value;
    }

    /// <summary>
    /// Gets or sets the delivery delay value.
    /// </summary>
    public TimeSpan DeliveryDelay
    {
        get => _producer.DeliveryDelay;
        set => _producer.DeliveryDelay = value;
    }

    /// <summary>
    /// Occurs when used.
    /// </summary>
    public event Action? Used;
}
