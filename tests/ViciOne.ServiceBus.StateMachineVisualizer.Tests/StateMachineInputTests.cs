using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Visualizer;
using Xunit;

namespace ViciOne.ServiceBus.StateMachineVisualizer.Tests;

public sealed class StateMachineInputTests
{
    private const string ExpectedGraphviz = """
        digraph G {
        0 [shape=ellipse, label="Initial"];
        1 [shape=ellipse, label="Running"];
        2 [shape=ellipse, label="Failed"];
        3 [shape=ellipse, label="Final"];
        4 [shape=ellipse, label="Suspended"];
        5 [shape=rectangle, label="Initialized"];
        6 [shape=rectangle, label="Exception"];
        7 [shape=rectangle, label="Finished"];
        8 [shape=rectangle, label="Suspend"];
        9 [shape=rectangle, label="Resume"];
        10 [shape=rectangle, label="Restart<RestartData>"];
        0 -> 5;
        1 -> 7;
        1 -> 8;
        2 -> 10;
        4 -> 9;
        5 -> 1;
        5 -> 6;
        6 -> 2;
        7 -> 3;
        8 -> 4;
        9 -> 1;
        10 -> 1;
        }
        """;

    private const string ExpectedMermaid = """
        flowchart TB;
            0(["Initial"]) --> 5["Initialized"];
            1(["Running"]) --> 7["Finished"];
            1(["Running"]) --> 8["Suspend"];
            2(["Failed"]) --> 10["Restart«RestartData»"];
            4(["Suspended"]) --> 9["Resume"];
            5["Initialized"] --> 1(["Running"]);
            5["Initialized"] --> 6["Exception"];
            6["Exception"] --> 2(["Failed"]);
            7["Finished"] --> 3(["Final"]);
            8["Suspend"] --> 4(["Suspended"]);
            9["Resume"] --> 1(["Running"]);
            10["Restart«RestartData»"] --> 1(["Running"]);
        """;

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "declarative-state-machine")]
    public void DeclarativeStateMachineGraph_RendersStatesEventsAndEdges()
    {
        var machine = new DeclarativeMachine();

        string output = new StateMachineGraphvizGenerator(machine.GetGraph()).Generate();

        Assert.Equal(StateMachineGraphFixtures.PlatformLines(ExpectedGraphviz), output);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "dynamic-state-machine")]
    public void DynamicStateMachineGraph_RendersStatesEventsAndEdges()
    {
        StateMachine<Instance> machine = ViciOneServiceBusStateMachine<Instance>.New(builder => builder
            .State("Running", out State running)
            .State("Suspended", out State suspended)
            .State("Failed", out State failed)
            .Event("Initialized", out Event initialized)
            .Event("Suspend", out Event suspend)
            .Event("Resume", out Event resume)
            .Event("Finished", out Event finished)
            .Event("Restart", out Event<RestartData> restart)
            .During(builder.Initial)
            .When(initialized, binder => binder
                .TransitionTo(running)
                .Catch<Exception>(handler => handler.TransitionTo(failed)))
            .During(running)
            .When(finished, binder => binder.Finalize())
            .When(suspend, binder => binder.TransitionTo(suspended))
            .Ignore(resume)
            .During(suspended)
            .When(resume, binder => binder.TransitionTo(running))
            .During(failed)
            .When(restart, context => context.Message.Name != null, binder => binder.TransitionTo(running)));

        string output = new StateMachineMermaidGenerator(machine.GetGraph()).Generate();

        Assert.Equal(StateMachineGraphFixtures.PlatformLines(ExpectedMermaid), output);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-VISUALIZER-INPUT", "request-derived-vertices")]
    public void RequestStateMachineGraph_RendersEveryRequestOutcome()
    {
        var machine = new RequestMachine();

        string output = new StateMachineGraphvizGenerator(machine.GetGraph()).Generate();

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
            During(
                Initial,
                When(Initialized)
                    .TransitionTo(Running)
                    .Catch<Exception>(handler => handler.TransitionTo(Failed)));
            During(
                Running,
                When(Finished).Finalize(),
                When(Suspend).TransitionTo(Suspended),
                Ignore(Resume));
            During(Suspended, When(Resume).TransitionTo(Running));
            During(Failed, When(Restart, context => context.Message.Name != null).TransitionTo(Running));
        }

        public State Running { get; private set; } = null!;

        public State Suspended { get; private set; } = null!;

        public State Failed { get; private set; } = null!;

        public Event Initialized { get; private set; } = null!;

        public Event Suspend { get; private set; } = null!;

        public Event Resume { get; private set; } = null!;

        public Event Finished { get; private set; } = null!;

        public Event<RestartData> Restart { get; private set; } = null!;
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
                    .Request(Process, context => context.InitAsync<RequestMessage>(new { context.Message.Id }))
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

    public sealed class RestartData
    {
        public string? Name { get; set; }
    }
}
