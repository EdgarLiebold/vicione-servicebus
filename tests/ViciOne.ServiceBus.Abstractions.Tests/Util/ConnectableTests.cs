using System.Collections.Concurrent;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Util;

public sealed class ConnectableTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONNECTABLE", "lifecycle-and-defensive-snapshot")]
    public void Connections_HaveIndependentIdempotentHandlesAndDefensiveSnapshots()
    {
        var first = new Connection("first");
        var second = new Connection("second");
        var outsider = new Connection("outsider");
        var connectable = new Connectable<Connection>();
        using ConnectHandle firstHandle = connectable.Connect(first);
        using ConnectHandle duplicateHandle = connectable.Connect(first);
        using ConnectHandle secondHandle = connectable.Connect(second);

        Connection[] exposed = connectable.Connected;
        exposed[0] = outsider;

        Assert.Equal(3, connectable.Count);
        Assert.NotSame(exposed, connectable.Connected);
        Assert.Equal(2, connectable.Connected.Count(connection => ReferenceEquals(connection, first)));
        Assert.Contains(second, connectable.Connected);
        Assert.DoesNotContain(outsider, connectable.Connected);

        firstHandle.Disconnect();
        firstHandle.Disconnect();
        firstHandle.Dispose();

        Assert.Equal(2, connectable.Count);
        Assert.Contains(first, connectable.Connected);
        Assert.Contains(second, connectable.Connected);

        duplicateHandle.Dispose();
        duplicateHandle.Dispose();

        Assert.Equal([second], connectable.Connected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONNECTABLE", "stable-async-dispatch-snapshot")]
    public async Task ForEachAsync_UsesOneStableSnapshotAndAwaitsEveryStartedCallbackAsync()
    {
        var first = new Connection("first");
        var second = new Connection("second");
        var third = new Connection("third");
        var connectable = new Connectable<Connection>();
        using ConnectHandle firstHandle = connectable.Connect(first);
        using ConnectHandle secondHandle = connectable.Connect(second);
        using ConnectHandle thirdHandle = connectable.Connect(third);
        var observed = new ConcurrentBag<Connection>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disconnectOnce = 0;

        Task dispatch = connectable.ForEachAsync(connection =>
        {
            observed.Add(connection);
            if (Interlocked.Exchange(ref disconnectOnce, 1) == 0)
                secondHandle.Disconnect();

            return ReferenceEquals(connection, first) ? release.Task : Task.CompletedTask;
        }, TestContext.Current.CancellationToken);

        Assert.Equal(3, observed.Count);
        Assert.Contains(first, observed);
        Assert.Contains(second, observed);
        Assert.Contains(third, observed);
        Assert.False(dispatch.IsCompleted);
        Assert.Equal(2, connectable.Count);

        release.SetResult();
        await dispatch;

        var nextDispatch = new List<Connection>();
        connectable.ForEach(nextDispatch.Add);
        Assert.Equal(2, nextDispatch.Count);
        Assert.Contains(first, nextDispatch);
        Assert.Contains(third, nextDispatch);
        Assert.DoesNotContain(second, nextDispatch);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONNECTABLE", "complete-async-failure-observation")]
    public async Task ForEachAsync_InvokesEveryConnectionAndReportsEveryFailureShapeAsync()
    {
        var synchronousFailure = new InvalidOperationException("synchronous");
        var asynchronousFailure = new ApplicationException("asynchronous");
        var connections = new[]
        {
            new Connection("synchronous"),
            new Connection("asynchronous"),
            new Connection("null-task"),
            new Connection("successful"),
        };
        var connectable = new Connectable<Connection>();
        var handles = connections.Select(connectable.Connect).ToArray();
        var observed = new ConcurrentBag<string>();

        try
        {
            Task dispatch = connectable.ForEachAsync(connection =>
            {
                observed.Add(connection.Name);
                return connection.Name switch
                {
                    "synchronous" => throw synchronousFailure,
                    "asynchronous" => Task.FromException(asynchronousFailure),
                    "null-task" => null!,
                    _ => Task.CompletedTask,
                };
            }, TestContext.Current.CancellationToken);

            await Assert.ThrowsAnyAsync<Exception>(() => dispatch);

            Assert.Equal(connections.Select(connection => connection.Name).Order(), observed.Order());
            AggregateException aggregate = Assert.IsType<AggregateException>(dispatch.Exception);
            IReadOnlyCollection<Exception> failures = aggregate.Flatten().InnerExceptions;
            Assert.Equal(3, failures.Count);
            Assert.Contains(synchronousFailure, failures);
            Assert.Contains(asynchronousFailure, failures);
            InvalidOperationException nullTaskFailure = Assert.Single(failures.OfType<InvalidOperationException>(),
                failure => !ReferenceEquals(failure, synchronousFailure));
            Assert.Equal("The connection callback returned a null task.", nullTaskFailure.Message);
        }
        finally
        {
            foreach (ConnectHandle handle in handles)
                handle.Dispose();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONNECTABLE", "all-short-circuit-contract")]
    public void All_UsesVacuousTruthAndStopsAtTheFirstRejectedConnection()
    {
        var connectable = new Connectable<Connection>();
        var evaluatedCount = 0;

        Assert.True(connectable.All(_ => false));

        using ConnectHandle first = connectable.Connect(new Connection("first"));
        using ConnectHandle second = connectable.Connect(new Connection("second"));
        using ConnectHandle third = connectable.Connect(new Connection("third"));

        bool accepted = connectable.All(_ =>
        {
            evaluatedCount++;
            return false;
        });

        Assert.False(accepted);
        Assert.Equal(1, evaluatedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONNECTABLE", "required-inputs")]
    public async Task PublicOperations_RejectEveryMissingRequiredInputBeforeInspectingConnectionsAsync()
    {
        var connectable = new Connectable<Connection>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Equal("connection", Assert.Throws<ArgumentNullException>(() => connectable.Connect(null!)).ParamName);
        Assert.Equal("callback", Assert.Throws<ArgumentNullException>(() => connectable.ForEach(null!)).ParamName);
        Assert.Equal("callback", Assert.Throws<ArgumentNullException>(() => connectable.All(null!)).ParamName);
        ArgumentNullException asynchronous = await Assert.ThrowsAsync<ArgumentNullException>(() => connectable.ForEachAsync(null!, TestContext.Current.CancellationToken));
        ArgumentNullException canceled = await Assert.ThrowsAsync<ArgumentNullException>(() => connectable.ForEachAsync(null!, cancellation.Token));
        Assert.Equal("callback", asynchronous.ParamName);
        Assert.Equal("callback", canceled.ParamName);
    }

    private sealed record Connection(string Name);
}
