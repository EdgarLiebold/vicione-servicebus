using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.StateMachineVisualizer.Tests;

internal static class StateMachineGraphFixtures
{
    internal static StateMachineGraph Canonical()
    {
        var initial = State("Initial");
        var running = State("Running");
        var failed = State("Failed");
        var final = State("Final");
        var suspended = State("Suspended");
        var initialized = Event("Initialized");
        var exception = ExceptionNode(typeof(Exception));
        var finished = Event("Finished");
        var suspend = Event("Suspend");
        var resume = Event("Resume");
        var restart = Event("Restart", typeof(RestartData));
        StateMachineGraphNode[] nodes =
        [
            initial,
            running,
            failed,
            final,
            suspended,
            initialized,
            exception,
            finished,
            suspend,
            resume,
            restart,
        ];
        StateMachineGraphEdge[] edges =
        [
            Connect(initial, initialized, StateMachineGraphEdgeKind.EventBinding),
            Connect(running, finished, StateMachineGraphEdgeKind.EventBinding),
            Connect(running, suspend, StateMachineGraphEdgeKind.EventBinding),
            Connect(failed, restart, StateMachineGraphEdgeKind.EventBinding),
            Connect(suspended, resume, StateMachineGraphEdgeKind.EventBinding),
            Connect(initialized, running, StateMachineGraphEdgeKind.StateTransition),
            Connect(initialized, exception, StateMachineGraphEdgeKind.ExceptionHandler),
            Connect(exception, failed, StateMachineGraphEdgeKind.StateTransition),
            Connect(finished, final, StateMachineGraphEdgeKind.StateTransition),
            Connect(suspend, suspended, StateMachineGraphEdgeKind.StateTransition),
            Connect(resume, running, StateMachineGraphEdgeKind.StateTransition),
            Connect(restart, running, StateMachineGraphEdgeKind.StateTransition),
        ];

        return new StateMachineGraph(nodes, edges);
    }

    internal static StateMachineGraph Composite(bool isComposite)
    {
        var initial = State("Initial");
        var waiting = State("Waiting");
        var final = State("Final");
        var start = Event("Start");
        var first = Event("First");
        var allReceived = Event("AllReceived", isComposite: isComposite);
        var second = Event("Second");
        var restart = Event("Restart");
        StateMachineGraphNode[] nodes = [initial, waiting, final, start, first, allReceived, second, restart];
        List<StateMachineGraphEdge> edges =
        [
            Connect(initial, start, StateMachineGraphEdgeKind.EventBinding),
            Connect(waiting, first, StateMachineGraphEdgeKind.EventBinding),
            Connect(waiting, second, StateMachineGraphEdgeKind.EventBinding),
            Connect(final, first, StateMachineGraphEdgeKind.EventBinding),
            Connect(final, second, StateMachineGraphEdgeKind.EventBinding),
            Connect(final, restart, StateMachineGraphEdgeKind.EventBinding),
            Connect(start, waiting, StateMachineGraphEdgeKind.StateTransition),
            Connect(allReceived, final, StateMachineGraphEdgeKind.StateTransition),
            Connect(restart, waiting, StateMachineGraphEdgeKind.StateTransition),
        ];
        if (isComposite)
        {
            edges.Add(Connect(first, allReceived, StateMachineGraphEdgeKind.CompositeContribution));
            edges.Add(Connect(second, allReceived, StateMachineGraphEdgeKind.CompositeContribution));
        }

        return new StateMachineGraph(nodes, edges);
    }

    internal static StateMachineGraph Disconnected()
    {
        var dormant = State("Dormant");
        var wake = Event("Wake");
        return new StateMachineGraph([dormant, wake], []);
    }

    internal static StateMachineGraph SyntaxSensitiveLabel()
    {
        var initial = State("State \" hash# <tag> & slash\\ tick` line\r\nnext");
        var completed = Event("Quote \" hash# <tag> & slash\\ tick` line\r\nnext");
        return new StateMachineGraph(
            [initial, completed],
            [Connect(initial, completed, StateMachineGraphEdgeKind.EventBinding)]);
    }

    internal static StateMachineGraph ControlCharacters()
    {
        var initial = State("State\nline\rtail\0\t\b\f\u001f");
        var completed = Event("Event\nline\rtail\0\t\b\f\u001f");
        return new StateMachineGraph(
            [initial, completed],
            [Connect(initial, completed, StateMachineGraphEdgeKind.EventBinding)]);
    }

    internal static StateMachineGraph UnicodeBoundary()
    {
        var initial = State("State 😀 high\ud800 low\udc00");
        var completed = Event("Event 😀 high\ud800 low\udc00");
        return new StateMachineGraph(
            [initial, completed],
            [Connect(initial, completed, StateMachineGraphEdgeKind.EventBinding)]);
    }

    internal static StateMachineGraph TypedEvents()
    {
        var initial = State("Initial");
        var received = Event("Received", typeof(Envelope<int>));
        var faulted = Event("Faulted", typeof(Fault<Envelope<string>>));
        var arrayReceived = Event("ArrayReceived", typeof(Envelope<int>[,]));
        var exception = ExceptionNode(typeof(GenericGraphFixtureException<Envelope<int>>));
        var nestedReceived = Event("NestedReceived", typeof(GenericGraphFixtureOuter<int>.Message<string>));
        var nestedException = ExceptionNode(typeof(GenericGraphFixtureOuter<int>.Failure<string>));
        var pairReceived = Event("PairReceived", typeof(Pair<int, string>));
        return new StateMachineGraph(
            [initial, received, faulted, arrayReceived, exception, nestedReceived, nestedException, pairReceived],
            [
                Connect(initial, received, StateMachineGraphEdgeKind.EventBinding),
                Connect(initial, faulted, StateMachineGraphEdgeKind.EventBinding),
                Connect(initial, arrayReceived, StateMachineGraphEdgeKind.EventBinding),
                Connect(received, exception, StateMachineGraphEdgeKind.ExceptionHandler),
                Connect(initial, nestedReceived, StateMachineGraphEdgeKind.EventBinding),
                Connect(nestedReceived, nestedException, StateMachineGraphEdgeKind.ExceptionHandler),
                Connect(initial, pairReceived, StateMachineGraphEdgeKind.EventBinding),
            ]);
    }

    internal static string CanonicalLines(string text) => text.ReplaceLineEndings("\n");

    private static StateMachineGraphNode State(string name) => StateMachineGraphNode.CreateState(name);

    private static StateMachineGraphNode Event(string name, Type? messageType = null, bool isComposite = false) =>
        StateMachineGraphNode.CreateEvent(name, messageType, isComposite);

    private static StateMachineGraphNode ExceptionNode(Type exceptionType) => StateMachineGraphNode.CreateException(exceptionType);

    private static StateMachineGraphEdge Connect(
        StateMachineGraphNode source,
        StateMachineGraphNode target,
        StateMachineGraphEdgeKind kind) => new(source, target, kind);
}
