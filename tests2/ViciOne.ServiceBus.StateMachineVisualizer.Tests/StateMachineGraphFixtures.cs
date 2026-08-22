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
        var exception = Event("Exception", typeof(Exception));
        var finished = Event("Finished");
        var suspend = Event("Suspend");
        var resume = Event("Resume");
        var restart = Event("Restart", typeof(RestartData));
        Vertex[] vertices =
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
        Edge[] edges =
        [
            Connect(initial, initialized),
            Connect(running, finished),
            Connect(running, suspend),
            Connect(failed, restart),
            Connect(suspended, resume),
            Connect(initialized, running),
            Connect(initialized, exception),
            Connect(exception, failed),
            Connect(finished, final),
            Connect(suspend, suspended),
            Connect(resume, running),
            Connect(restart, running),
        ];

        return new StateMachineGraph(vertices, edges);
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
        Vertex[] vertices = [initial, waiting, final, start, first, allReceived, second];
        Edge[] edges =
        [
            Connect(initial, start),
            Connect(waiting, first),
            Connect(waiting, second),
            Connect(final, first),
            Connect(final, second),
            Connect(start, waiting),
            Connect(first, allReceived),
            Connect(allReceived, final),
            Connect(second, allReceived),
        ];

        return new StateMachineGraph(vertices, edges);
    }

    internal static string PlatformLines(string text) =>
        text.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", Environment.NewLine, StringComparison.Ordinal);

    private static Vertex State(string title) => new(typeof(State), typeof(State), title, false);

    private static Vertex Event(string title, Type? targetType = null, bool isComposite = false) =>
        new(typeof(Event), targetType ?? typeof(Event), title, isComposite);

    private static Edge Connect(Vertex from, Vertex to) => new(from, to, to.Title);

    internal sealed class RestartData;
}
