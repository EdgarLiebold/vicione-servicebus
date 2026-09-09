using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class BackgroundWorkOwnershipTests
{
    private static readonly string[] DirectLoopOwners =
    [
        "src/Transports/ViciOne.ServiceBus.AmazonSqs/AmazonSqsTransport/Batcher.cs",
        "src/Transports/ViciOne.ServiceBus.AmazonSqs/AmazonSqsTransport/Middleware/AmazonSqsMessageReceiver.cs",
        "src/Transports/ViciOne.ServiceBus.EventHubs/EventHubIntegration/Checkpoints/BatchCheckpointer.cs",
        "src/ViciOne.ServiceBus.JobService/Runtime/JobProgressBuffer.cs",
        "src/ViciOne.ServiceBus/SqlTransport/Middleware/SqlMessageReceiver.cs",
        "src/ViciOne.ServiceBus/SqlTransport/SqlReceiveLockContext.cs",
        "src/ViciOne.ServiceBus/Transports/Fabric/MessageQueue.cs",
    ];

    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-AGENT-OWNERSHIP", "agent-creation-call-sites-use-owned-bridge")]
    public void AgentCreationCallSites_UseTheOwnedStartBridge()
    {
        string[] offenders = Directory.GetFiles(Path.Combine(RepositoryLayout.Root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.EndsWith("SupervisorExtensions.cs", StringComparison.Ordinal))
            .SelectMany(file => File.ReadLines(file)
                .Select((line, index) => (Line: line.TrimStart(), Number: index + 1))
                .Where(item => item.Line.Contains(".CreateAgent(", StringComparison.Ordinal)
                    && !item.Line.StartsWith("return ", StringComparison.Ordinal))
                .Select(item => $"{RepositoryLayout.RelativeToRoot(file)}:{item.Number}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(offenders);

        string bridge = Source("src/ViciOne.ServiceBus/Advanced/Middleware/SupervisorExtensions.cs");
        int bridgeStart = bridge.IndexOf("public static void StartAgent", StringComparison.Ordinal);
        int bridgeEnd = bridge.IndexOf("public static async Task<TAgent> CreateAgent", bridgeStart, StringComparison.Ordinal);
        Assert.True(bridgeStart >= 0);
        Assert.True(bridgeEnd > bridgeStart);
        string startBridge = bridge[bridgeStart..bridgeEnd];
        Assert.Contains("Task<TAgent> creationTask = supervisor.CreateAgent", startBridge, StringComparison.Ordinal);
        Assert.Contains("creationTask.GetAwaiter().OnCompleted", startBridge, StringComparison.Ordinal);
        Assert.Contains("creationTask.GetAwaiter().GetResult();", startBridge, StringComparison.Ordinal);
        Assert.Contains("catch", startBridge, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-TASK-OWNERSHIP", "long-running-loops-have-direct-lifecycle-owner")]
    public void LongRunningLoops_StartDirectlyAndKeepTheirTaskInALifecycleField()
    {
        foreach (string relativePath in DirectLoopOwners)
        {
            string source = Source(relativePath);

            Assert.DoesNotContain("Task.Run(", source, StringComparison.Ordinal);
        }

        string consumerAgent = Source("src/ViciOne.ServiceBus/Transports/ConsumerAgent.cs");
        Assert.DoesNotContain("Task.Run(", consumerAgent, StringComparison.Ordinal);
        Assert.DoesNotContain(".ContinueWith(", consumerAgent, StringComparison.Ordinal);
        Assert.Contains("_consumeTaskObserver = ObserveConsumeTaskAsync(consumeTask);", consumerAgent, StringComparison.Ordinal);

        string jobService = Source("src/ViciOne.ServiceBus.JobService/Runtime/JobService.cs");
        Assert.DoesNotContain(".ContinueWith(", jobService, StringComparison.Ordinal);
        Assert.Contains("_jobCompletions.Add(CompleteJobAsync(jobHandle));", jobService, StringComparison.Ordinal);

        string batchConnector = Source("src/ViciOne.ServiceBus/Consumers/Configuration/BatchConsumerMessageConnector.cs");
        Assert.DoesNotContain("Task.Run(", batchConnector, StringComparison.Ordinal);
        Assert.Contains("public ValueTask DisposeAsync() => new(BeginDisconnectAsync());", batchConnector, StringComparison.Ordinal);
        Assert.Contains("_disposeTask = CompleteDisconnectAsync(disconnectFailure, factoryCleanup);", batchConnector, StringComparison.Ordinal);
        Assert.Contains("_ = ObserveCleanupFailureAsync(cleanup);", batchConnector, StringComparison.Ordinal);

        string gauge = Source("src/ViciOne.ServiceBus/Transports/Fabric/Gauge.cs");
        Assert.DoesNotContain("Task.Run(", gauge, StringComparison.Ordinal);
        Assert.Contains("public Task RemoveAsync(CancellationToken cancellationToken = default)", gauge, StringComparison.Ordinal);

        string activeMqConsumer = Source(
            "src/Transports/ViciOne.ServiceBus.ActiveMq/ActiveMqTransport/Middleware/ActiveMqConsumerFilter.cs");
        Assert.DoesNotContain(".ContinueWith(", activeMqConsumer, StringComparison.Ordinal);
        Assert.Contains("Task? connectionStopTask", activeMqConsumer, StringComparison.Ordinal);
        Assert.Contains("connectionStopTask = StopAfterConnectionExceptionAsync(exception);", activeMqConsumer, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-CALLBACK-OWNERSHIP", "off-thread-callback-transitions-transfer-outcome")]
    public void CallbackDrivenOffThreadTransitions_StoreOrTransferEveryOutcome()
    {
        string[] activeMqOwners =
        [
            "src/Transports/ViciOne.ServiceBus.ActiveMq/ActiveMqTransport/ConnectionContextFactory.cs",
            "src/Transports/ViciOne.ServiceBus.ActiveMq/ActiveMqTransport/ScopeSessionContextFactory.cs",
            "src/Transports/ViciOne.ServiceBus.ActiveMq/ActiveMqTransport/SessionContextFactory.cs",
        ];
        foreach (string relativePath in activeMqOwners.Skip(1))
        {
            string source = Source(relativePath);
            Assert.Contains("Task? faultStopTask", source, StringComparison.Ordinal);
            Assert.Contains("faultStopTask = StopAfterConnectionExceptionAsync(exception);", source, StringComparison.Ordinal);
            Assert.Contains("await context", source, StringComparison.OrdinalIgnoreCase);
        }

        string activeMqConnection = Source(activeMqOwners[0]);
        Assert.Contains("Task? faultStopTask", activeMqConnection, StringComparison.Ordinal);
        Assert.Contains("faultStopTask = stopCompletion.Task;", activeMqConnection, StringComparison.Ordinal);
        Assert.Contains("_ = StopAfterConnectionExceptionAsync(exception, stopCompletion);", activeMqConnection, StringComparison.Ordinal);
        Assert.Contains("await contextHandle.Stop", activeMqConnection, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.Yield", activeMqConnection, StringComparison.Ordinal);

        string[] azureOwners =
        [
            "src/Transports/ViciOne.ServiceBus.AzureServiceBus/AzureServiceBusTransport/Contexts/QueueClientContext.cs",
            "src/Transports/ViciOne.ServiceBus.AzureServiceBus/AzureServiceBusTransport/Contexts/SubscriptionClientContext.cs",
        ];
        foreach (string relativePath in azureOwners)
        {
            string source = Source(relativePath);
            Assert.Contains("Task? _faultStopTask", source, StringComparison.Ordinal);
            Assert.Contains("_faultStopTask = StopAfterCallbackAsync(entityPath);", source, StringComparison.Ordinal);
            Assert.Contains("await _agent.Stop", source, StringComparison.Ordinal);
        }

        (string Path, string ReturnedStop)[] rabbitCallbackOwners =
        [
            (
                "src/Transports/ViciOne.ServiceBus.RabbitMq/RabbitMqTransport/ChannelContextFactory.cs",
                "return asyncContext.StopAsync(args.ReplyText);"
            ),
            (
                "src/Transports/ViciOne.ServiceBus.RabbitMq/RabbitMqTransport/ConnectionContextFactory.cs",
                "return contextHandle.StopAsync(args.ReplyText);"
            ),
        ];
        foreach ((string relativePath, string returnedStop) in rabbitCallbackOwners)
        {
            string source = Source(relativePath);
            Assert.DoesNotContain("Task.Run(", source, StringComparison.Ordinal);
            Assert.DoesNotContain(".ContinueWith(", source, StringComparison.Ordinal);
            Assert.Contains(returnedStop, source, StringComparison.Ordinal);
        }

        string rabbitChannel = Source(
            "src/Transports/ViciOne.ServiceBus.RabbitMq/RabbitMqTransport/RabbitMqChannelContext.cs");
        Assert.Contains("Task? _faultStopTask", rabbitChannel, StringComparison.Ordinal);
        Assert.Contains("_faultStopTask = StopAfterCallbackAsync(inputAddress);", rabbitChannel, StringComparison.Ordinal);
        Assert.Contains("await _agent.Stop", rabbitChannel, StringComparison.Ordinal);

        string rabbit = Source("src/Transports/ViciOne.ServiceBus.RabbitMq/RabbitMqTransport/TransportLifetime.cs");
        Assert.Contains("_scheduleSubjectDisposal(DisposeSubjectAsync);", rabbit, StringComparison.Ordinal);
        Assert.Contains("var failure = await _disposed.Task.ConfigureAwait(false);", rabbit, StringComparison.Ordinal);
        Assert.Contains("_disposed.TrySetResult(failure);", rabbit, StringComparison.Ordinal);
        Assert.Contains("ExceptionDispatchInfo.Capture(failure).Throw();", rabbit, StringComparison.Ordinal);
    }

    private static string Source(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryLayout.Root, relativePath));
}
