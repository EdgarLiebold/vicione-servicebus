using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides extension methods for send callback.</summary>
public static class SendCallbackExtensions
{
    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga> Send<TSaga, TMessage>(this EventActivityBinder<TSaga> source, Uri destinationAddress,
        TMessage message, SendContextCallback<TSaga, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TMessage>(_ => destinationAddress, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga> SendAwaited<TSaga, TMessage>(this EventActivityBinder<TSaga> source, Uri destinationAddress,
        Task<TMessage> message, SendContextCallback<TSaga, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TMessage>(_ => destinationAddress, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga> Send<TSaga, TMessage>(this EventActivityBinder<TSaga> source,
        DestinationAddressProvider<TSaga> destinationAddressProvider, TMessage message, SendContextCallback<TSaga, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TMessage>(destinationAddressProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga> SendAwaited<TSaga, TMessage>(this EventActivityBinder<TSaga> source,
        DestinationAddressProvider<TSaga> destinationAddressProvider, Task<TMessage> message, SendContextCallback<TSaga, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TMessage>(destinationAddressProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga> Send<TSaga, TMessage>(this EventActivityBinder<TSaga> source, Uri destinationAddress,
        EventMessageFactory<TSaga, TMessage> messageFactory, SendContextCallback<TSaga, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TMessage>(_ => destinationAddress, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga> SendAwaited<TSaga, TMessage>(this EventActivityBinder<TSaga> source, Uri destinationAddress,
        AsyncEventMessageFactory<TSaga, TMessage> messageFactory, SendContextCallback<TSaga, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TMessage>(_ => destinationAddress, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga> SendAwaited<TSaga, TMessage>(this EventActivityBinder<TSaga> source, Uri destinationAddress,
        Func<BehaviorContext<TSaga>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory, SendContextCallback<TSaga, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TMessage>(_ => destinationAddress, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga> Send<TSaga, TMessage>(this EventActivityBinder<TSaga> source,
        DestinationAddressProvider<TSaga> destinationAddressProvider, EventMessageFactory<TSaga, TMessage> messageFactory,
        SendContextCallback<TSaga, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TMessage>(destinationAddressProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga> SendAwaited<TSaga, TMessage>(this EventActivityBinder<TSaga> source,
        DestinationAddressProvider<TSaga> destinationAddressProvider, AsyncEventMessageFactory<TSaga, TMessage> messageFactory,
        SendContextCallback<TSaga, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TMessage>(destinationAddressProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga> SendAwaited<TSaga, TMessage>(this EventActivityBinder<TSaga> source,
        DestinationAddressProvider<TSaga> destinationAddressProvider, Func<BehaviorContext<TSaga>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        SendContextCallback<TSaga, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TMessage>(destinationAddressProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga, TData> Send<TSaga, TData, TMessage>(this EventActivityBinder<TSaga, TData> source,
        Uri destinationAddress, TMessage message, SendContextCallback<TSaga, TData, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TData, TMessage>(_ => destinationAddress, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga, TData> SendAwaited<TSaga, TData, TMessage>(this EventActivityBinder<TSaga, TData> source,
        Uri destinationAddress, Task<TMessage> message, SendContextCallback<TSaga, TData, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TData, TMessage>(_ => destinationAddress, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga, TData> Send<TSaga, TData, TMessage>(this EventActivityBinder<TSaga, TData> source,
        DestinationAddressProvider<TSaga, TData> destinationAddressProvider, TMessage message,
        SendContextCallback<TSaga, TData, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TData, TMessage>(destinationAddressProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga, TData> SendAwaited<TSaga, TData, TMessage>(this EventActivityBinder<TSaga, TData> source,
        DestinationAddressProvider<TSaga, TData> destinationAddressProvider, Task<TMessage> message,
        SendContextCallback<TSaga, TData, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TData, TMessage>(destinationAddressProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga, TData> Send<TSaga, TData, TMessage>(this EventActivityBinder<TSaga, TData> source,
        Uri destinationAddress, EventMessageFactory<TSaga, TData, TMessage> messageFactory,
        SendContextCallback<TSaga, TData, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TData, TMessage>(_ => destinationAddress, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga, TData> SendAwaited<TSaga, TData, TMessage>(this EventActivityBinder<TSaga, TData> source,
        Uri destinationAddress, AsyncEventMessageFactory<TSaga, TData, TMessage> messageFactory,
        SendContextCallback<TSaga, TData, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TData, TMessage>(_ => destinationAddress, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga, TData> SendAwaited<TSaga, TData, TMessage>(this EventActivityBinder<TSaga, TData> source,
        Uri destinationAddress, Func<BehaviorContext<TSaga, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        SendContextCallback<TSaga, TData, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TData, TMessage>(_ => destinationAddress, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga, TData> Send<TSaga, TData, TMessage>(this EventActivityBinder<TSaga, TData> source,
        DestinationAddressProvider<TSaga, TData> destinationAddressProvider, EventMessageFactory<TSaga, TData, TMessage> messageFactory,
        SendContextCallback<TSaga, TData, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TData, TMessage>(destinationAddressProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga, TData> SendAwaited<TSaga, TData, TMessage>(this EventActivityBinder<TSaga, TData> source,
        DestinationAddressProvider<TSaga, TData> destinationAddressProvider,
        AsyncEventMessageFactory<TSaga, TData, TMessage> messageFactory,
        SendContextCallback<TSaga, TData, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TData, TMessage>(destinationAddressProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga, TData> SendAwaited<TSaga, TData, TMessage>(this EventActivityBinder<TSaga, TData> source,
        DestinationAddressProvider<TSaga, TData> destinationAddressProvider,
        Func<BehaviorContext<TSaga, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        SendContextCallback<TSaga, TData, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TSaga, TData, TMessage>(destinationAddressProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TException> Send<TSaga, TException, TMessage>(this ExceptionActivityBinder<TSaga, TException> source,
        Uri destinationAddress, TMessage message, SendExceptionContextCallback<TSaga, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(
            new FaultedSendActivity<TSaga, TException, TMessage>(_ => destinationAddress, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TException> SendAwaited<TSaga, TException, TMessage>(this ExceptionActivityBinder<TSaga, TException> source,
        Uri destinationAddress, Task<TMessage> message, SendExceptionContextCallback<TSaga, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(
            new FaultedSendActivity<TSaga, TException, TMessage>(_ => destinationAddress, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TException> Send<TSaga, TException, TMessage>(this ExceptionActivityBinder<TSaga, TException> source,
        DestinationAddressProvider<TSaga> destinationAddressProvider, TMessage message,
        SendExceptionContextCallback<TSaga, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(
            new FaultedSendActivity<TSaga, TException, TMessage>(destinationAddressProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TException> SendAwaited<TSaga, TException, TMessage>(this ExceptionActivityBinder<TSaga, TException> source,
        DestinationAddressProvider<TSaga> destinationAddressProvider, Task<TMessage> message,
        SendExceptionContextCallback<TSaga, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(
            new FaultedSendActivity<TSaga, TException, TMessage>(destinationAddressProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TException> Send<TSaga, TException, TMessage>(this ExceptionActivityBinder<TSaga, TException> source,
        Uri destinationAddress,
        EventExceptionMessageFactory<TSaga, TException, TMessage> messageFactory, SendExceptionContextCallback<TSaga, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TException, TMessage>(_ => destinationAddress,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TException> Send<TSaga, TException, TMessage>(this ExceptionActivityBinder<TSaga, TException> source,
        DestinationAddressProvider<TSaga> destinationAddressProvider,
        EventExceptionMessageFactory<TSaga, TException, TMessage> messageFactory,
        SendExceptionContextCallback<TSaga, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TException, TMessage>(destinationAddressProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TException> SendAwaited<TSaga, TException, TMessage>(this ExceptionActivityBinder<TSaga, TException> source,
        Uri destinationAddress,
        AsyncEventExceptionMessageFactory<TSaga, TException, TMessage> messageFactory, SendExceptionContextCallback<TSaga, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TException, TMessage>(_ => destinationAddress,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TException> SendAwaited<TSaga, TException, TMessage>(this ExceptionActivityBinder<TSaga, TException> source,
        Uri destinationAddress,
        Func<BehaviorExceptionContext<TSaga, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        SendExceptionContextCallback<TSaga, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TException, TMessage>(_ => destinationAddress,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TException> SendAwaited<TSaga, TException, TMessage>(this ExceptionActivityBinder<TSaga, TException> source,
        DestinationAddressProvider<TSaga> destinationAddressProvider,
        AsyncEventExceptionMessageFactory<TSaga, TException, TMessage> messageFactory,
        SendExceptionContextCallback<TSaga, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TException, TMessage>(destinationAddressProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TException> SendAwaited<TSaga, TException, TMessage>(this ExceptionActivityBinder<TSaga, TException> source,
        DestinationAddressProvider<TSaga> destinationAddressProvider,
        Func<BehaviorExceptionContext<TSaga, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        SendExceptionContextCallback<TSaga, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TException, TMessage>(destinationAddressProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TData, TException> Send<TSaga, TData, TException, TMessage>(
        this ExceptionActivityBinder<TSaga, TData, TException> source, Uri destinationAddress, TMessage message,
        SendExceptionContextCallback<TSaga, TData, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(
            new FaultedSendActivity<TSaga, TData, TException, TMessage>(_ => destinationAddress, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TData, TException> SendAwaited<TSaga, TData, TException, TMessage>(
        this ExceptionActivityBinder<TSaga, TData, TException> source, Uri destinationAddress, Task<TMessage> message,
        SendExceptionContextCallback<TSaga, TData, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TData, TException, TMessage>(_ => destinationAddress,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TData, TException> Send<TSaga, TData, TException, TMessage>(
        this ExceptionActivityBinder<TSaga, TData, TException> source, DestinationAddressProvider<TSaga, TData> destinationAddressProvider,
        TMessage message, SendExceptionContextCallback<TSaga, TData, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TData, TException, TMessage>(destinationAddressProvider,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TData, TException> SendAwaited<TSaga, TData, TException, TMessage>(
        this ExceptionActivityBinder<TSaga, TData, TException> source, DestinationAddressProvider<TSaga, TData> destinationAddressProvider,
        Task<TMessage> message, SendExceptionContextCallback<TSaga, TData, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TData, TException, TMessage>(destinationAddressProvider,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TData, TException> Send<TSaga, TData, TException, TMessage>(
        this ExceptionActivityBinder<TSaga, TData, TException> source, Uri destinationAddress,
        EventExceptionMessageFactory<TSaga, TData, TException, TMessage> messageFactory,
        SendExceptionContextCallback<TSaga, TData, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TData, TException, TMessage>(_ => destinationAddress,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TData, TException> SendAwaited<TSaga, TData, TException, TMessage>(
        this ExceptionActivityBinder<TSaga, TData, TException> source, Uri destinationAddress,
        AsyncEventExceptionMessageFactory<TSaga, TData, TException, TMessage> messageFactory,
        SendExceptionContextCallback<TSaga, TData, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TData, TException, TMessage>(_ => destinationAddress,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TData, TException> SendAwaited<TSaga, TData, TException, TMessage>(
        this ExceptionActivityBinder<TSaga, TData, TException> source, Uri destinationAddress,
        Func<BehaviorExceptionContext<TSaga, TData, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        SendExceptionContextCallback<TSaga, TData, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TData, TException, TMessage>(_ => destinationAddress,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TData, TException> Send<TSaga, TData, TException, TMessage>(
        this ExceptionActivityBinder<TSaga, TData, TException> source,
        DestinationAddressProvider<TSaga, TData> destinationAddressProvider,
        EventExceptionMessageFactory<TSaga, TData, TException, TMessage> messageFactory,
        SendExceptionContextCallback<TSaga, TData, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TData, TException, TMessage>(destinationAddressProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TData, TException> SendAwaited<TSaga, TData, TException, TMessage>(
        this ExceptionActivityBinder<TSaga, TData, TException> source,
        DestinationAddressProvider<TSaga, TData> destinationAddressProvider,
        AsyncEventExceptionMessageFactory<TSaga, TData, TException, TMessage> messageFactory,
        SendExceptionContextCallback<TSaga, TData, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TData, TException, TMessage>(destinationAddressProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="destinationAddressProvider">The destination address provider.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TData, TException> SendAwaited<TSaga, TData, TException, TMessage>(
        this ExceptionActivityBinder<TSaga, TData, TException> source,
        DestinationAddressProvider<TSaga, TData> destinationAddressProvider,
        Func<BehaviorExceptionContext<TSaga, TData, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        SendExceptionContextCallback<TSaga, TData, TException, TMessage> callback)
        where TSaga : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TSaga, TData, TException, TMessage>(destinationAddressProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }
}
