using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Visualizer;
using Xunit;

namespace ViciOne.ServiceBus.StateMachineVisualizer.Tests;

public sealed class StateMachineInputTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "declarative-state-machine")]
    public void DeclarativeStateMachineGraph_RendersStatesEventsAndEdges()
    {
        var machine = new DeclarativeMachine();

        string output = new StateMachineGraphvizGenerator(machine.GetGraph()).CreateDotFile();

        Assert.Contains("label=\"Initial\"", output, StringComparison.Ordinal);
        Assert.Contains("label=\"Running\"", output, StringComparison.Ordinal);
        Assert.Contains("label=\"Start\"", output, StringComparison.Ordinal);
        Assert.Contains("label=\"Finish\"", output, StringComparison.Ordinal);
        Assert.Contains(" -> ", output, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "dynamic-state-machine")]
    public void DynamicStateMachineGraph_RendersStatesEventsAndEdges()
    {
        StateMachine<Instance> machine = ViciOneServiceBusStateMachine<Instance>.New(builder => builder
            .State("Running", out State running)
            .Event("Start", out Event start)
            .Event("Finish", out Event finish)
            .During(builder.Initial)
            .When(start, binder => binder.TransitionTo(running))
            .During(running)
            .When(finish, binder => binder.Finalize()));

        string output = new StateMachineMermaidGenerator(machine.GetGraph()).CreateMermaidFile();

        Assert.Contains("Initial", output, StringComparison.Ordinal);
        Assert.Contains("Running", output, StringComparison.Ordinal);
        Assert.Contains("Start", output, StringComparison.Ordinal);
        Assert.Contains("Finish", output, StringComparison.Ordinal);
        Assert.Contains(" --> ", output, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "request-derived-vertices")]
    public void RequestStateMachineGraph_RendersEveryRequestOutcome()
    {
        var machine = new RequestMachine();

        string output = new StateMachineGraphvizGenerator(machine.GetGraph()).CreateDotFile();

        Assert.Contains(machine.Process.Pending.Name, output, StringComparison.Ordinal);
        Assert.Contains(machine.Process.Completed.Name, output, StringComparison.Ordinal);
        Assert.Contains(machine.Process.Faulted.Name, output, StringComparison.Ordinal);
        Assert.Contains(machine.Process.TimeoutExpired.Name, output, StringComparison.Ordinal);
        Assert.Contains(machine.Completed.Name, output, StringComparison.Ordinal);
        Assert.Contains(machine.Failed.Name, output, StringComparison.Ordinal);
        Assert.Contains(machine.TimedOut.Name, output, StringComparison.Ordinal);
    }

    private sealed class DeclarativeMachine : ViciOneServiceBusStateMachine<Instance>
    {
        public DeclarativeMachine()
        {
            Initially(When(Start).TransitionTo(Running));
            During(Running, When(Finish).Finalize());
        }

        public State Running { get; private set; } = null!;

        public Event Start { get; private set; } = null!;

        public Event Finish { get; private set; } = null!;
    }

    private sealed class Instance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public State CurrentState { get; set; } = null!;
    }

    private sealed class RequestMachine : ViciOneServiceBusStateMachine<RequestInstance>
    {
        public RequestMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Request(() => Process, instance => instance.RequestId);

            Initially(
                When(Start)
                    .Request(Process, context => context.Init<RequestMessage>(new { context.Message.Id }))
                    .TransitionTo(Process.Pending));
            During(
                Process.Pending,
                When(Process.Completed).TransitionTo(Completed),
                When(Process.Faulted).TransitionTo(Failed),
                When(Process.TimeoutExpired).TransitionTo(TimedOut));
        }

        public State Completed { get; private set; } = null!;

        public State Failed { get; private set; } = null!;

        public State TimedOut { get; private set; } = null!;

        public Event<StartRequest> Start { get; private set; } = null!;

        public Request<RequestInstance, RequestMessage, RequestResponse> Process { get; private set; } = null!;
    }

    private sealed class RequestInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public Guid? RequestId { get; set; }
    }

    public sealed class StartRequest
    {
        public Guid Id { get; set; }
    }

    public sealed class RequestMessage
    {
        public Guid Id { get; set; }
    }

    public sealed class RequestResponse;
}
