using System.Collections.Concurrent;
using System.Reflection;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsReceiverPollingTests
{
    private const string QueueName = "orders";
    private const string QueueUrl = "https://sqs.eu-central-1.amazonaws.com/123456789012/orders";
    private const string QueueArn = "arn:aws:sqs:eu-central-1:123456789012:orders";
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData("45", 45)]
    [InlineData("30", 30)]
    [InlineData("invalid", 30)]
    [InlineData(null, 30)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "receiver-resolves-queue-before-poll-and-adopts-visibility")]
    public Task Receiver_ResolvesQueueBeforePollingAndUsesValidProviderVisibilityAsync(
        string? providerVisibility, int expectedVisibility) =>
        VerifyPollingAsync(providerVisibility, expectedVisibility, successfulPollOnStop: false);

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "successful-empty-poll-after-stop-does-not-repoll")]
    public async Task Receiver_SuccessfulEmptyPollAfterStopCompletesWithoutPollingAgainAsync()
    {
        ILogContext? previous = LogContext.Current;
        var logger = new StopLogger();
        LogContext.ConfigureCurrentLogContext(logger);
        try
        {
            await VerifyPollingAsync("45", 45, successfulPollOnStop: true);
            Assert.Empty(logger.WarningsAndErrors);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    private static async Task VerifyPollingAsync(string? providerVisibility, int expectedVisibility, bool successfulPollOnStop)
    {
        QueueReceiveSettings settings = CreateSettings();
        settings.WaitTimeSeconds = 7;
        settings.VisibilityTimeout = 30;
        var queueLookup = new TaskCompletionSource<QueueInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lookupEntered = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pollEntered = new TaskCompletionSource<(string Name, int Limit, int Wait)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var polls = 0;
        var successfulPolls = 0;
        CancellationToken observedPollToken = default;
        IAmazonSQS sqs = InterfaceProxy<IAmazonSQS>.Create((method, _) => throw new NotSupportedException(method.Name));
        var attributes = new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn };
        if (providerVisibility is not null)
            attributes[QueueAttributeName.VisibilityTimeout] = providerVisibility;
        await using var queue = new QueueInfo(QueueName, QueueUrl, attributes, sqs, CancellationToken.None, true);
        ClientContext client = InterfaceProxy<ClientContext>.Create((method, args) => method.Name switch
        {
            nameof(PipeContext.TryGetPayload) => TryGetPayload(method, args),
            nameof(ClientContext.GetQueueInfoAsync) => LookupAsync(args),
            nameof(ClientContext.ReceiveMessagesAsync) => PollAsync(args),
            _ => throw new NotSupportedException(method.Name)
        });
        IReceivePipeDispatcher dispatcher = InterfaceProxy<IReceivePipeDispatcher>.Create((method, _) => method.Name switch
        {
            "add_ZeroActivity" or "remove_ZeroActivity" => null,
            "get_ActiveDispatchCount" => 0,
            "get_DispatchCount" => 0L,
            "get_MaxConcurrentDispatchCount" => 0,
            _ => throw new NotSupportedException(method.Name)
        });
        ILogContext? logContext = null;
        logContext = InterfaceProxy<ILogContext>.Create((method, _) => method.Name switch
        {
            "get_Logger" => NullLogger.Instance,
            "get_Messages" or nameof(ILogContext.CreateLogContext) => logContext,
            _ => null
        });
        SqsReceiveEndpointContext endpoint = InterfaceProxy<SqsReceiveEndpointContext>.Create((method, args) => method.Name switch
        {
            nameof(PipeContext.TryGetPayload) => NoPayload(args),
            nameof(ReceiveEndpointContext.CreateReceivePipeDispatcher) => dispatcher,
            "get_InputAddress" => new Uri("amazonsqs://eu-central-1/orders"),
            "get_LogContext" => logContext,
            "get_StopTimeout" => OperationTimeout,
            "get_ConsumerStopTimeout" => null,
            _ => throw new NotSupportedException(method.Name)
        });

        var receiver = new AmazonSqsMessageReceiver(client, endpoint);
        try
        {
            Assert.Equal(QueueName, await lookupEntered.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
            Assert.False(receiver.Ready.IsCompleted);
            Assert.Equal(0, Volatile.Read(ref polls));

            queueLookup.TrySetResult(queue);
            await receiver.Ready.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            (string name, int limit, int wait) = await pollEntered.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

            Assert.Equal(QueueUrl, settings.QueueUrl);
            Assert.Equal(expectedVisibility, settings.VisibilityTimeout);
            Assert.Equal(QueueName, name);
            Assert.True(settings.PrefetchCount >= 10);
            Assert.True(settings.ConcurrentMessageLimit >= 10);
            Assert.Equal(10, limit);
            Assert.Equal(7, wait);
        }
        finally
        {
            queueLookup.TrySetResult(queue);
            await receiver.StopAsync(TestContext.Current.CancellationToken).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        }

        Assert.True(receiver.Stopped.IsCancellationRequested);
        Assert.True(observedPollToken.IsCancellationRequested);
        Assert.True(receiver.Completed.IsCompletedSuccessfully);
        Assert.Equal(1, Volatile.Read(ref polls));
        Assert.Equal(successfulPollOnStop ? 1 : 0, Volatile.Read(ref successfulPolls));

        object? TryGetPayload(MethodInfo method, object?[]? args)
        {
            Assert.NotNull(args);
            if (method.GetGenericArguments()[0] != typeof(ReceiveSettings))
            {
                args[0] = null;
                return false;
            }

            args[0] = settings;
            return true;
        }

        Task<QueueInfo> LookupAsync(object?[]? args)
        {
            Assert.NotNull(args);
            lookupEntered.TrySetResult(Assert.IsType<string>(args[0]));
            return queueLookup.Task;
        }

        async Task<IList<Message>> PollAsync(object?[]? args)
        {
            Assert.NotNull(args);
            Interlocked.Increment(ref polls);
            observedPollToken = Assert.IsType<CancellationToken>(args[3]);
            pollEntered.TrySetResult((Assert.IsType<string>(args[0]), Assert.IsType<int>(args[1]), Assert.IsType<int>(args[2])));
            if (successfulPollOnStop)
            {
                var stopping = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using CancellationTokenRegistration registration = observedPollToken.Register(() => stopping.TrySetResult());
                await stopping.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
                Interlocked.Increment(ref successfulPolls);
            }
            else
                await Task.Delay(Timeout.InfiniteTimeSpan, observedPollToken);
            return Array.Empty<Message>();
        }
    }

    private static object NoPayload(object?[]? args)
    {
        Assert.NotNull(args);
        args[0] = null;
        return false;
    }

    private static QueueReceiveSettings CreateSettings()
    {
        var topology = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var parent = new AmazonSqsEndpointConfiguration(topology);
        return new QueueReceiveSettings(parent.CreateEndpointConfiguration(false), QueueName, true, false);
    }

    private sealed class StopLogger : ILogger
    {
        public ConcurrentQueue<string> WarningsAndErrors { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning)
                WarningsAndErrors.Enqueue(formatter(state, exception));
        }
    }
}
