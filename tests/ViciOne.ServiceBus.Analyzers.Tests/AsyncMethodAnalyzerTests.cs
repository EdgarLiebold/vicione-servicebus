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
            "Method IPublishEndpoint.PublishAsync<OrderSubmitted>() is not awaited or captured and may result in message loss",
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
            "Method ISendEndpoint.SendAsync<OrderSubmitted>() is not awaited or captured and may result in message loss",
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
            "Method IAdvancedRequestClient<SubmitOrder>.GetResponseAsync<OrderSubmitted>() is not awaited or captured and may result in message loss",
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
            "Method ConsumeContext.RespondAsync<OrderSubmitted>() is not awaited or captured and may result in message loss",
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
