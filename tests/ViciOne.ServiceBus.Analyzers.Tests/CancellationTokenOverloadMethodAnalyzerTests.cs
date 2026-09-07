using Microsoft.CodeAnalysis;
using ViciOne.ServiceBus.Analyzers.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.Diagnostics;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.Tests;

public sealed class CancellationTokenOverloadMethodAnalyzerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "task-delay-overload")]
    public async Task TaskDelayWithoutToken_ReportsTheContextTokenOverloadAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Consumer :
        IConsumer<SubmitOrder>
    {
        public Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            return Task.Delay(10);
        }
    }
}
";

        await AssertDiagnosticsAsync(
            source,
            new DiagnosticObservation(
                "VOSB2001",
                DiagnosticSeverity.Info,
                "Cancellation token from 'context.CancellationToken' can be used in cancellation token overload for 'Task.Delay' method",
                "Test0.cs",
                30,
                20));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "state-machine-activity-overloads")]
    public async Task StateMachineActivity_ReportsEachAvailableContextTokenAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{

        class TestInstance :
            SagaStateMachineInstance
        {
            public Guid CorrelationId { get; set; }
            public State CurrentState { get; set; }
            public string Value { get; set; }
        }

        class SetValueAsyncActivity :
            IStateMachineActivity<TestInstance, SubmitOrder>
        {

        Task IStateMachineActivity<TestInstance, SubmitOrder>.ExecuteAsync(BehaviorContext<TestInstance, SubmitOrder> context,
            IBehavior<TestInstance, SubmitOrder> next)
        {
            return Task.Delay(10);
        }

        Task IStateMachineActivity<TestInstance, SubmitOrder>.FaultedAsync<TException>(BehaviorExceptionContext<TestInstance, SubmitOrder, TException> ctx,
            IBehavior<TestInstance, SubmitOrder> next)
        {
            return Task.Run(() => next.FaultedAsync(ctx));
        }

        public void Accept(StateMachineVisitor visitor)
        {
            visitor.Visit(this);
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
";

        await AssertDiagnosticsAsync(
            source,
            new DiagnosticObservation(
                "VOSB2001",
                DiagnosticSeverity.Info,
                "Cancellation token from 'context.CancellationToken' can be used in cancellation token overload for 'Task.Delay' method",
                "Test0.cs",
                41,
                20),
            new DiagnosticObservation(
                "VOSB2001",
                DiagnosticSeverity.Info,
                "Cancellation token from 'ctx.CancellationToken' can be used in cancellation token overload for 'Task.Run' method",
                "Test0.cs",
                47,
                20));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "later-task-run-overload")]
    public async Task ExistingTokenUse_DoesNotSuppressASeparateMissingTokenAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Consumer :
        IConsumer<SubmitOrder>
    {
        public async Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            await Task.Delay(10, context.CancellationToken);
            context.Advanced().RespondAsync<OrderSubmitted>(context.Message);
            await Task.Run(() => {});

        }
    }
}
";

        await AssertDiagnosticsAsync(
            source,
            new DiagnosticObservation(
                "VOSB2001",
                DiagnosticSeverity.Info,
                "Cancellation token from 'context.CancellationToken' can be used in cancellation token overload for 'Task.Run' method",
                "Test0.cs",
                32,
                19));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "existing-token-is-silent")]
    public async Task ExistingCancellationToken_DoesNotReportAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Consumer :
        IConsumer<SubmitOrder>
    {
        public async Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            await Task.Delay(10, context.CancellationToken);
            context.Advanced().RespondAsync<OrderSubmitted>(context.Message);
        }
    }
}
";

        await AssertDiagnosticsAsync(source);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "servicebus-method-is-excluded")]
    public async Task ServiceBusPublishMethod_DoesNotSuggestItsOwnContextTokenAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Consumer :
        IConsumer<SubmitOrder>
    {
        public Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            return context.Outgoing.PublishAsync(context.Message);
        }
    }
}
";

        await AssertDiagnosticsAsync(source);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "incompatible-overload-shapes-are-silent")]
    public async Task ReorderedAndRefIncompatibleOverloads_DoNotReportAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    static class Operations
    {
        public static Task Reordered(int value, string label) => Task.CompletedTask;
        public static Task Reordered(string label, System.Threading.CancellationToken cancellationToken, int value) => Task.CompletedTask;

        public static Task RefSensitive(ref int value) => Task.CompletedTask;
        public static Task RefSensitive(int value, System.Threading.CancellationToken cancellationToken) => Task.CompletedTask;
    }

    class Consumer : IConsumer<SubmitOrder>
    {
        public Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            var value = 42;
            Operations.Reordered(value, ""label"");
            return Operations.RefSensitive(ref value);
        }
    }
}
";

        await AssertDiagnosticsAsync(source);
    }

    private static async Task AssertDiagnosticsAsync(
        string source,
        params DiagnosticObservation[] expected)
    {
        var actual = await RoslynTestHost.AnalyzeAsync(
            source,
            new CancellationTokenOverloadMethodAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        ServiceBusAnalyzerFixture.AssertDiagnostics(actual, expected);
    }
}
