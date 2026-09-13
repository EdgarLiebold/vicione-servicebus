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
                "Forward cancellation token 'context.CancellationToken' to the cancellable overload of 'Task.Delay'",
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
            ISagaStateMachineInstance
        {
            public Guid CorrelationId { get; set; }
            public IState CurrentState { get; set; }
            public string Value { get; set; }
        }

        class SetValueAsyncActivity :
            IStateMachineActivity<TestInstance, SubmitOrder>
        {

        Task IStateMachineActivity<TestInstance, SubmitOrder>.ExecuteAsync(IBehaviorContext<TestInstance, SubmitOrder> context,
            IBehavior<TestInstance, SubmitOrder> next)
        {
            return Task.Delay(10);
        }

        Task IStateMachineActivity<TestInstance, SubmitOrder>.FaultedAsync<TException>(IBehaviorExceptionContext<TestInstance, SubmitOrder, TException> ctx,
            IBehavior<TestInstance, SubmitOrder> next)
        {
            return Task.Run(() => next.FaultedAsync(ctx));
        }

        public void Accept(IStateMachineVisitor visitor)
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
                "Forward cancellation token 'context.CancellationToken' to the cancellable overload of 'Task.Delay'",
                "Test0.cs",
                41,
                20),
            new DiagnosticObservation(
                "VOSB2001",
                DiagnosticSeverity.Info,
                "Forward cancellation token 'ctx.CancellationToken' to the cancellable overload of 'Task.Run'",
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
                "Forward cancellation token 'context.CancellationToken' to the cancellable overload of 'Task.Run'",
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
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "inherited-context-method-is-excluded")]
    public async Task InheritedConsumeContextMethod_DoesNotSuggestItsOwnOptionalTokenAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    abstract class DerivedContext : ViciOne.ServiceBus.Context.BaseConsumeContext
    {
        protected DerivedContext()
            : base(default!, default!)
        {
        }
    }

    static class ContextOperations
    {
        public static Task NotifyAsync(DerivedContext context)
        {
            return context.NotifyConsumedAsync<SubmitOrder>(default!, TimeSpan.Zero, ""consumer"");
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

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "lambda-and-outer-context-tokens")]
    public async Task NestedLambda_ReportsItsOwnAndTheOuterContextTokensAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    class Consumer : IConsumer<SubmitOrder>
    {
        public Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            return Invoke(inner => Task.Delay(10));
        }

        static Task Invoke(Func<ConsumeContext<SubmitOrder>, Task> callback) =>
            callback(default!);
    }
}
";

        var diagnostic = Assert.Single(await AnalyzeAsync(source));
        Assert.Equal("VOSB2001", diagnostic.Id);
        Assert.Contains("context.CancellationToken,inner.CancellationToken", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "accessor-constructor-and-local-function-contexts")]
    public async Task AccessorsConstructorAndLocalFunction_ExposeTheirAvailableContextTokensAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    sealed class ContextOwner
    {
        public ContextOwner(ConsumeContext<SubmitOrder> context)
        {
            Task.Delay(1);
        }

        public ConsumeContext<SubmitOrder> Current
        {
            set { Task.Delay(2); }
        }

        public ConsumeContext<SubmitOrder> this[int index]
        {
            set { Task.Delay(3); }
        }

        public Task Run(ConsumeContext<SubmitOrder> context)
        {
            Task Local(ConsumeContext<SubmitOrder> nested) => Task.Delay(4);
            return Local(context);
        }
    }
}
";

        var diagnostics = await AnalyzeAsync(source);

        Assert.True(
            diagnostics.Count == 4,
            $"Expected four diagnostics, actual: {string.Join(" | ", diagnostics.Select(diagnostic => $"{diagnostic.Line}:{diagnostic.Message}"))}");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Message.Contains("context.CancellationToken", StringComparison.Ordinal));
        Assert.Equal(2, diagnostics.Count(diagnostic => diagnostic.Message.Contains("value.CancellationToken", StringComparison.Ordinal)));
        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Message.Contains("context.CancellationToken,nested.CancellationToken", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CANCELLATION-TOKEN-ANALYZER", "extension-method-overload")]
    public async Task ExtensionMethodWithTokenOverload_ReportsTheAvailableContextTokenAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + ServiceBusAnalyzerFixture.SimpleMessageContracts + @"
namespace ConsoleApplication1
{
    static class WorkExtensions
    {
        public static Task WorkAsync(this string value) => Task.CompletedTask;
        public static Task WorkAsync(this string value, System.Threading.CancellationToken cancellationToken) => Task.CompletedTask;
    }

    class Consumer : IConsumer<SubmitOrder>
    {
        public Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            return ""value"".WorkAsync();
        }
    }
}
";

        var diagnostic = Assert.Single(await AnalyzeAsync(source));
        Assert.Equal("VOSB2001", diagnostic.Id);
        Assert.Contains("context.CancellationToken", diagnostic.Message, StringComparison.Ordinal);
    }

    private static Task<IReadOnlyList<ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.Diagnostics.DiagnosticObservation>> AnalyzeAsync(
        string source) =>
        RoslynTestHost.AnalyzeAsync(
            source,
            new CancellationTokenOverloadMethodAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

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
