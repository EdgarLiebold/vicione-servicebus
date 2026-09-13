using Microsoft.CodeAnalysis;
using ViciOne.ServiceBus.Analyzers.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.Diagnostics;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.Tests;

public sealed class AsyncMethodAnalyzerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "publish-must-be-observed")]
    public async Task PublishWithoutAwaitOrCapture_ReportsMessageLossRiskAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            bus.PublishAsync<OrderSubmitted>(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });

            await bus.PublishAsync<OrderSubmitted>(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });
        }
    }
}
";

        await AssertSingleDiagnosticAsync(
            source,
            "Observe the task returned by 'IPublishEndpoint.PublishAsync<OrderSubmitted>()' to prevent message loss",
            line: 31,
            column: 13);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "send-must-be-observed")]
    public async Task SendWithoutAwaitOrCapture_ReportsMessageLossRiskAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            var endpoint = await bus.GetSendEndpointAsync(new Uri(""loopback://localhost/input_queue""));
            endpoint.SendAsync<OrderSubmitted>(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });

            await endpoint.SendAsync<OrderSubmitted>(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });
        }
    }
}
";

        await AssertSingleDiagnosticAsync(
            source,
            "Observe the task returned by 'ISendEndpoint.SendAsync<OrderSubmitted>()' to prevent message loss",
            line: 32,
            column: 13);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "request-must-be-observed")]
    public async Task GetResponseWithoutAwaitOrCapture_ReportsMessageLossRiskAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            var client = bus.CreateRequestClient<SubmitOrder>();
            client.Advanced().GetResponseAsync<OrderSubmitted>(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });

            var response = await client.Advanced().GetResponseAsync<OrderSubmitted>(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });
        }
    }
}
";

        await AssertSingleDiagnosticAsync(
            source,
            "Observe the task returned by 'IAdvancedRequestClient<SubmitOrder>.GetResponseAsync<OrderSubmitted>()' to prevent message loss",
            line: 32,
            column: 13);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "response-must-be-observed")]
    public async Task RespondWithoutAwaitOrCapture_ReportsMessageLossRiskAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Consumer :
        IConsumer<SubmitOrder>
    {
        public async Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            context.Advanced().RespondAsync<OrderSubmitted>(context.Message);
        }
    }

    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            var client = bus.CreateRequestClient<SubmitOrder>();
            var response = await client.Advanced().GetResponseAsync<OrderSubmitted>(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });
        }
    }
}
";

        await AssertSingleDiagnosticAsync(
            source,
            "Observe the task returned by 'ConsumeContext.RespondAsync<OrderSubmitted>()' to prevent message loss",
            line: 30,
            column: 13);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "non-task-create-is-excluded")]
    public async Task RequestHandleCreation_DoesNotReportAsyncMessageLossRiskAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            var client = bus.CreateRequestClient<SubmitOrder>();
            client.Create(new
            {
                Id = NewId.NextGuid(),
                CustomerId = ""427"",
            });
        }
    }
}
";

        var actual = await RoslynTestHost.AnalyzeAsync(
            source,
            new AsyncMethodAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        Assert.Empty(actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "lookalike-producer-is-excluded")]
    public async Task SourceDefinedLookalikeProducer_DoesNotReportAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + @"
namespace ViciOne.ServiceBus.Advanced
{
    public static class ForwardExtensions
    {
        public static Task ForwardAsync() => Task.CompletedTask;
    }
}

namespace ConsoleApplication1
{
    class Program
    {
        static void Main()
        {
            ViciOne.ServiceBus.Advanced.ForwardExtensions.ForwardAsync();
        }
    }
}
";

        var actual = await RoslynTestHost.AnalyzeAsync(
            source,
            new AsyncMethodAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        Assert.Empty(actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "runtime-contract-initializers-must-be-observed")]
    public async Task RuntimeContractInitializerCalls_RequireObservationAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
            var endpoint = await bus.GetSendEndpointAsync(new Uri(""loopback://localhost/input_queue""));

            endpoint.SendAsync(typeof(OrderSubmitted), new { Id = Guid.Empty, CustomerId = ""427"" });
            bus.PublishAsync(typeof(OrderSubmitted), new { Id = Guid.Empty, CustomerId = ""427"" });
        }
    }
}
";

        var actual = await RoslynTestHost.AnalyzeAsync(
            source,
            new AsyncMethodAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        Assert.Equal(2, actual.Count);
        Assert.Contains(actual, diagnostic => diagnostic.Message.Contains("SendAsync", StringComparison.Ordinal));
        Assert.Contains(actual, diagnostic => diagnostic.Message.Contains("PublishAsync", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "destination-schedule-must-be-observed")]
    public async Task DestinationScheduleWithoutObservation_ReportsMessageLossRiskAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Consumer : IConsumer<SubmitOrder>
    {
        public Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            context.Advanced().ScheduleSendAsync<OrderSubmitted>(
                new Uri(""loopback://localhost/input_queue""),
                DateTimeOffset.UtcNow,
                new { Id = Guid.Empty, CustomerId = ""427"" });
            return Task.CompletedTask;
        }
    }
}
";

        var actual = await RoslynTestHost.AnalyzeAsync(
            source,
            new AsyncMethodAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(actual);
        Assert.Equal("VOSB1001", diagnostic.Id);
        Assert.Contains("ScheduleSendAsync", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "advanced-and-application-producer-families")]
    public async Task AdvancedAndApplicationProducerFamilies_RequireObservationAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + @"
using ViciOne.ServiceBus.Advanced.Middleware;

namespace ConsoleApplication1
{
    public sealed class Request { }
    public sealed class Response { }

    class Program
    {
        static void Produce(
            IPublishEndpoint publisher,
            ISendEndpoint sender,
            IMessageScheduler scheduler,
            IRequestClient<Request> client,
            IOutgoingMessages outgoing)
        {
            publisher.Advanced().PublishAsync(new Request(), (IPipe<PublishContext<Request>>)null!);
            AdvancedPublishEndpointExtensions.PublishAsync(publisher, new Request(), (IPipe<PublishContext<Request>>)null!);
            sender.Advanced().SendAsync(new Request(), (IPipe<SendContext<Request>>)null!);
            AdvancedSendEndpointExtensions.SendAsync(sender, new Request(), (IPipe<SendContext<Request>>)null!);
            scheduler.Advanced().ScheduleSendAsync(new Uri(""loopback://localhost/input""), DateTimeOffset.UtcNow, new Request(), (IPipe<SendContext<Request>>)null!);
            AdvancedMessageSchedulerExtensions.ScheduleSendAsync(scheduler, new Uri(""loopback://localhost/input""), DateTimeOffset.UtcNow, new Request(), (IPipe<SendContext<Request>>)null!);
            scheduler.Advanced().SchedulePublishAsync(DateTimeOffset.UtcNow, new Request(), (IPipe<SendContext<Request>>)null!);
            AdvancedMessageSchedulerExtensions.SchedulePublishAsync(scheduler, DateTimeOffset.UtcNow, new Request(), (IPipe<SendContext<Request>>)null!);
            AdvancedRequestClientExtensions.GetResponseAsync<Request, Response>(client, new Request(), default(RequestTimeout));
            outgoing.SendAsync(new Request());
            outgoing.PublishAsync(new Request());
            outgoing.ScheduleSendAsync(new Uri(""loopback://localhost/input""), DateTimeOffset.UtcNow, new Request());
        }
    }
}
";

        var actual = await RoslynTestHost.AnalyzeAsync(
            source,
            new AsyncMethodAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        Assert.Equal(12, actual.Count);
        Assert.All(actual, diagnostic => Assert.Equal("VOSB1001", diagnostic.Id));
        Assert.Equal(3, actual.Count(diagnostic => diagnostic.Message.Contains(".SendAsync", StringComparison.Ordinal)));
        Assert.Equal(3, actual.Count(diagnostic => diagnostic.Message.Contains(".PublishAsync", StringComparison.Ordinal)));
        Assert.Equal(3, actual.Count(diagnostic => diagnostic.Message.Contains(".ScheduleSendAsync", StringComparison.Ordinal)));
        Assert.Equal(2, actual.Count(diagnostic => diagnostic.Message.Contains(".SchedulePublishAsync", StringComparison.Ordinal)));
        Assert.Single(actual, diagnostic => diagnostic.Message.Contains(".GetResponseAsync", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-METHOD-ANALYZER", "discarded-and-configured-tasks-are-unobserved")]
    public async Task DiscardedAndConfiguredProducerTasks_ReportMessageLossRiskAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            _ = bus.PublishAsync(new OrderSubmittedMessage());
            bus.PublishAsync(new OrderSubmittedMessage()).ConfigureAwait(false);
            _ = (bus.PublishAsync(new OrderSubmittedMessage())!);
            _ = (bus.PublishAsync(new OrderSubmittedMessage())!).ConfigureAwait(false);

            Task captured = bus.PublishAsync(new OrderSubmittedMessage());
            await captured;
        }

        static async Task CaptureInUnderscoreVariable(IPublishEndpoint bus)
        {
            Task _ = bus.PublishAsync(new OrderSubmittedMessage());
            await _;
        }
    }

    public sealed class OrderSubmittedMessage { }
}
";

        var actual = await RoslynTestHost.AnalyzeAsync(
            source,
            new AsyncMethodAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        Assert.Equal(4, actual.Count);
        Assert.All(actual, diagnostic => Assert.Equal("VOSB1001", diagnostic.Id));
    }

    private static async Task AssertSingleDiagnosticAsync(
        string source,
        string message,
        int line,
        int column)
    {
        var actual = await RoslynTestHost.AnalyzeAsync(
            source,
            new AsyncMethodAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        ServiceBusAnalyzerFixture.AssertDiagnostics(
            actual,
            new DiagnosticObservation(
                "VOSB1001",
                DiagnosticSeverity.Warning,
                message,
                "Test0.cs",
                line,
                column));
    }
}
