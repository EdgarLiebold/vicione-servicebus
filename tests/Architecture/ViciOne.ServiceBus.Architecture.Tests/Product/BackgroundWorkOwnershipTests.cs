using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class BackgroundWorkOwnershipTests
{
    private static readonly string[] DirectLoopOwners =
    [
        "src/Transports/ViciOne.ServiceBus.AmazonSqsTransport/AmazonSqsTransport/Batcher.cs",
        "src/Transports/ViciOne.ServiceBus.AmazonSqsTransport/AmazonSqsTransport/Middleware/AmazonSqsMessageReceiver.cs",
        "src/Transports/ViciOne.ServiceBus.EventHubIntegration/EventHubIntegration/Checkpoints/BatchCheckpointer.cs",
        "src/ViciOne.ServiceBus/JobService/JobService/JobProgressBuffer.cs",
        "src/ViciOne.ServiceBus/SqlTransport/SqlTransport/Middleware/SqlMessageReceiver.cs",
        "src/ViciOne.ServiceBus/SqlTransport/SqlTransport/SqlReceiveLockContext.cs",
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

        string bridge = Source("src/ViciOne.ServiceBus/SupervisorExtensions.cs");
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
        Assert.Contains("_consumeTaskObserver = ObserveConsumeTask(consumeTask);", consumerAgent, StringComparison.Ordinal);

        string jobService = Source("src/ViciOne.ServiceBus/JobService/JobService/JobService.cs");
        Assert.DoesNotContain(".ContinueWith(", jobService, StringComparison.Ordinal);
        Assert.Contains("_jobCompletions.Add(CompleteJob(jobHandle));", jobService, StringComparison.Ordinal);

        string batchConnector = Source("src/ViciOne.ServiceBus/Consumers/Configuration/BatchConsumerMessageConnector.cs");
        Assert.DoesNotContain("Task.Run(", batchConnector, StringComparison.Ordinal);
        Assert.Contains("_disposeTask = DisposeConsumerFactory();", batchConnector, StringComparison.Ordinal);

        string gauge = Source("src/ViciOne.ServiceBus/Transports/Fabric/Gauge.cs");
        Assert.DoesNotContain("Task.Run(", gauge, StringComparison.Ordinal);
        Assert.Contains("public Task Remove()", gauge, StringComparison.Ordinal);

        string activeMqConsumer = Source(
            "src/Transports/ViciOne.ServiceBus.ActiveMqTransport/ActiveMqTransport/Middleware/ActiveMqConsumerFilter.cs");
        Assert.DoesNotContain(".ContinueWith(", activeMqConsumer, StringComparison.Ordinal);
        Assert.Contains("Task connectionStopTask", activeMqConsumer, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-CALLBACK-OWNERSHIP", "off-thread-callback-transitions-transfer-outcome")]
    public void CallbackDrivenOffThreadTransitions_StoreOrTransferEveryOutcome()
    {
        string[] activeMqOwners =
        [
            "src/Transports/ViciOne.ServiceBus.ActiveMqTransport/ActiveMqTransport/ConnectionContextFactory.cs",
            "src/Transports/ViciOne.ServiceBus.ActiveMqTransport/ActiveMqTransport/ScopeSessionContextFactory.cs",
            "src/Transports/ViciOne.ServiceBus.ActiveMqTransport/ActiveMqTransport/SessionContextFactory.cs",
        ];
        foreach (string relativePath in activeMqOwners.Skip(1))
        {
            string source = Source(relativePath);
            Assert.Contains("Task faultStopTask", source, StringComparison.Ordinal);
            Assert.Contains("faultStopTask = StopAfterConnectionException(exception);", source, StringComparison.Ordinal);
            Assert.Contains("await context", source, StringComparison.OrdinalIgnoreCase);
        }

        string activeMqConnection = Source(activeMqOwners[0]);
        Assert.Contains("Task faultStopTask", activeMqConnection, StringComparison.Ordinal);
        Assert.Contains("faultStopTask = stopCompletion.Task;", activeMqConnection, StringComparison.Ordinal);
        Assert.Contains("_ = StopAfterConnectionException(exception, stopCompletion);", activeMqConnection, StringComparison.Ordinal);
        Assert.Contains("await contextHandle.Stop", activeMqConnection, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.Yield", activeMqConnection, StringComparison.Ordinal);

        string[] azureOwners =
        [
            "src/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core/AzureServiceBusTransport/Contexts/QueueClientContext.cs",
            "src/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core/AzureServiceBusTransport/Contexts/SubscriptionClientContext.cs",
        ];
        foreach (string relativePath in azureOwners)
        {
            string source = Source(relativePath);
            Assert.Contains("Task _faultStopTask", source, StringComparison.Ordinal);
            Assert.Contains("_faultStopTask = StopAfterCallback(entityPath);", source, StringComparison.Ordinal);
            Assert.Contains("await _agent.Stop", source, StringComparison.Ordinal);
        }

        (string Path, string ReturnedStop)[] rabbitCallbackOwners =
        [
            (
                "src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqTransport/ChannelContextFactory.cs",
                "return asyncContext.Stop(args.ReplyText);"
            ),
            (
                "src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqTransport/ConnectionContextFactory.cs",
                "return contextHandle.Stop(args.ReplyText);"
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
            "src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqTransport/RabbitMqChannelContext.cs");
        Assert.Contains("Task _faultStopTask", rabbitChannel, StringComparison.Ordinal);
        Assert.Contains("_faultStopTask = StopAfterCallback(inputAddress);", rabbitChannel, StringComparison.Ordinal);
        Assert.Contains("await _agent.Stop", rabbitChannel, StringComparison.Ordinal);

        string rabbit = Source("src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqTransport/TransportLifetime.cs");
        Assert.Contains("_scheduleSubjectDisposal(DisposeSubject);", rabbit, StringComparison.Ordinal);
        Assert.Contains("var failure = await _disposed.Task.ConfigureAwait(false);", rabbit, StringComparison.Ordinal);
        Assert.Contains("_disposed.TrySetResult(failure);", rabbit, StringComparison.Ordinal);
        Assert.Contains("ExceptionDispatchInfo.Capture(failure).Throw();", rabbit, StringComparison.Ordinal);
    }

    private static string Source(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryLayout.Root, relativePath));
}
