using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AmazonSqs.Middleware;
/// <summary>
/// Receives messages from AmazonSQS, pushing them to the InboundPipe of the service endpoint.
/// </summary>
public sealed class AmazonSqsMessageReceiver :
    ConsumerAgent<string>
{
    readonly ClientContext _client;
    readonly SqsReceiveEndpointContext _context;
    readonly IPartitionedTaskExecutor<Message> _executorPool;
    readonly ReceiveSettings _receiveSettings;

    /// <summary>
    /// Polls Amazon SQS for messages and dispatches them to the receive pipeline.
    /// </summary>
    /// <param name="client">The model context for the consumer</param>
    /// <param name="context">The topology</param>
    public AmazonSqsMessageReceiver(ClientContext client, SqsReceiveEndpointContext context)
        : base(context, StringComparer.Ordinal)
    {
        _client = client;
        _context = context;

        _receiveSettings = client.GetPayload<ReceiveSettings>();

        _executorPool = new FifoPartitionedTaskExecutor(_receiveSettings);

        TrySetConsumeTask(ConsumeAsync());
    }

    /// <summary>
    /// Performs the active and actual agents completed operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    protected override async Task ActiveAndActualAgentsCompletedAsync(StopContext context)
    {
        await base.ActiveAndActualAgentsCompletedAsync(context).ConfigureAwait(false);

        await _executorPool.DisposeAsync().ConfigureAwait(false);
    }

    async Task ConsumeAsync()
    {
        await GetQueueAttributesAsync(Stopping).ConfigureAwait(false);

        using var algorithm = new RequestRateAlgorithm(new RequestRateAlgorithmOptions
        {
            PrefetchCount = _receiveSettings.PrefetchCount,
            ConcurrentResultLimit = _receiveSettings.ConcurrentMessageLimit,
            RequestResultLimit = 10
        }, _context.GetTimeProvider());

        SetReady();

        Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            var lockContext = new AmazonSqsReceiveLockContext(_context.InputAddress, message, _receiveSettings, _client, Stopped);

            return _receiveSettings.IsOrdered
                ? _executorPool.EnqueueAsync(message, () => HandleMessageAsync(message, lockContext), cancellationToken)
                : HandleMessageAsync(message, lockContext);
        }

        try
        {
            while (!IsStopping)
            {
                if (_receiveSettings is { IsOrdered: true, ConcurrentDeliveryLimit: 1 })
                {
                    await algorithm.RunAsync(
                            ReceiveMessagesAsync,
                            (message, cancellationToken) => HandleAsync(message, cancellationToken),
                            GroupByMessageGroup,
                            OrderBySequenceNumber,
                            Stopping)
                        .ConfigureAwait(false);
                }
                else
                    await algorithm.RunAsync(ReceiveMessagesAsync, (message, cancellationToken) => HandleAsync(message, cancellationToken), Stopping)
                        .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (Stopping.IsCancellationRequested)
        {
        }
    }

    async Task GetQueueAttributesAsync(CancellationToken cancellationToken)
    {
        var queueInfo = await _client.GetQueueInfoAsync(_receiveSettings.EntityName, cancellationToken).ConfigureAwait(false);

        _receiveSettings.QueueUrl = queueInfo.Url;

        if (queueInfo.Attributes != null && queueInfo.Attributes.TryGetValue(QueueAttributeName.VisibilityTimeout, out var value)
            && int.TryParse(value, out var visibilityTimeout)
            && visibilityTimeout != _receiveSettings.VisibilityTimeout)
        {
            LogContext.Debug?.Log("Using queue visibility timeout of {VisibilityTimeout}", TimeSpan.FromSeconds(visibilityTimeout).ToFriendlyString());

            _receiveSettings.VisibilityTimeout = visibilityTimeout;
        }
    }

    async Task HandleMessageAsync(Message message, ReceiveLockContext lockContext)
    {
        if (IsStopping)
            return;

        var redelivered = message.Attributes != null && message.Attributes.TryGetInt("ApproximateReceiveCount", out var receiveCount) && receiveCount > 1;

        var context = new AmazonSqsReceiveContext(message, redelivered, _context, _client, _receiveSettings, _client.ConnectionContext);
        try
        {
            await DispatchAsync(message.MessageId, context, lockContext).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            context.LogTransportFaulted(exception);
        }
        finally
        {
            context.Dispose();
        }
    }

    async Task<IEnumerable<Message>> ReceiveMessagesAsync(int messageLimit, CancellationToken cancellationToken)
    {
        return await ReceiveMessagesAsync(
                token => _client.ReceiveMessagesAsync(_receiveSettings.EntityName, messageLimit, _receiveSettings.WaitTimeSeconds, token),
                cancellationToken)
            .ConfigureAwait(false);
    }

    internal static async Task<IEnumerable<Message>> ReceiveMessagesAsync(
        Func<CancellationToken, Task<IList<Message>>> receive,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receive);

        return await receive(cancellationToken).ConfigureAwait(false);
    }

    static IEnumerable<IGrouping<string, Message>> GroupByMessageGroup(IEnumerable<Message> messages)
    {
        return messages.GroupBy(GetMessageGroupId, StringComparer.Ordinal);
    }

    static IEnumerable<Message> OrderBySequenceNumber(IEnumerable<Message> messages)
    {
        return messages.OrderBy(GetSequenceNumber);
    }

    static string GetMessageGroupId(Message message)
    {
        if (message.Attributes != null
            && message.Attributes.TryGetValue(MessageSystemAttributeName.MessageGroupId, out var groupId)
            && !string.IsNullOrWhiteSpace(groupId))
            return groupId;

        throw new InvalidDataException("An ordered Amazon SQS message is missing its MessageGroupId system attribute.");
    }

    static BigInteger GetSequenceNumber(Message message)
    {
        if (message.Attributes != null
            && message.Attributes.TryGetValue(MessageSystemAttributeName.SequenceNumber, out var sequenceNumber)
            && BigInteger.TryParse(sequenceNumber, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        throw new InvalidDataException("An ordered Amazon SQS message is missing a valid SequenceNumber system attribute.");
    }


    class FifoPartitionedTaskExecutor :
        IPartitionedTaskExecutor<Message>
    {
        readonly IPartitionedTaskExecutor<Message> _keyExecutorPool;

        public FifoPartitionedTaskExecutor(ReceiveSettings receiveSettings)
        {
            IHashGenerator hashGenerator = new Murmur3UnsafeHashGenerator();
            int partitionCapacity = Math.Max(
                1,
                (receiveSettings.PrefetchCount + receiveSettings.ConcurrentMessageLimit - 1) / receiveSettings.ConcurrentMessageLimit);
            _keyExecutorPool = new PartitionedTaskExecutor<Message>(MessageGroupIdProvider, hashGenerator,
                receiveSettings.ConcurrentMessageLimit, receiveSettings.ConcurrentDeliveryLimit, partitionCapacity);
        }

        public Task EnqueueAsync(Message result, Func<Task> handle, CancellationToken cancellationToken)
        {
            return _keyExecutorPool.EnqueueAsync(result, handle, cancellationToken);
        }

        public Task ExecuteAsync(Message result, Func<Task> method, CancellationToken cancellationToken = default)
        {
            return _keyExecutorPool.ExecuteAsync(result, method, cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return _keyExecutorPool.DisposeAsync();
        }

        static byte[] MessageGroupIdProvider(Message message)
        {
            return Encoding.UTF8.GetBytes(GetMessageGroupId(message));
        }
    }
}
