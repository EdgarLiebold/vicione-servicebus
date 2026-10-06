using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Logging;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class BackplanePublicationLifetimeTests
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);

    public static TheoryData<string, string> Cases
    {
        get
        {
            var cases = new TheoryData<string, string>();
            foreach (string route in new[] { "connections", "groups", "users" })
            foreach (string outcome in new[]
                { "admission", "null-task", "faults-cleanup", "canceled-prefix", "async-fault", "success", "first-throw" })
                cases.Add(route, outcome);
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task FanOut_JoinsActualPublicationsBeforeReleasingTheirRealScopeAsync(string route, string outcome)
    {
        ILogContext? previousLogContext = LogContext.Current;
        var scenario = new PublicationScenario(outcome);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHubProtocolResolver>(new TestHubProtocolResolver([new JsonHubProtocol()]));
        services.AddViciOneServiceBus(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.AddSignalRBackplane<TestHub>();
            registration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri($"loopback://signalr-lifetime-{NewId.NextGuid():N}/"));
                bus.ConfigureEndpoints(context);
            });
        });
        services.AddScoped<IPublishEndpoint>(provider => new ObservedPublishEndpoint(
            provider.GetRequiredService<Bind<IBus, IPublishEndpoint>>().Value, provider, scenario));

        ServiceProvider provider = services.BuildServiceProvider();
        IBusControl? bus = null;
        HubLifetimeManager<TestHub>? manager = null;
        var clients = new[]
        {
            new HubConnectionTestClient("user-a"),
            new HubConnectionTestClient("user-b"),
            new HubConnectionTestClient("user-c"),
        };
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task? operation = null;
        Exception? primary = null;
        var cleanupFailures = new List<Exception>();
        try
        {
            bus = provider.GetRequiredService<IBusControl>();
            manager = provider.GetRequiredService<HubLifetimeManager<TestHub>>();
            await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Deadline, TestContext.Current.CancellationToken);
            for (var index = 0; index < clients.Length; index++)
            {
                await manager.OnConnectedAsync(clients[index].HubConnection);
                await manager.AddToGroupAsync(clients[index].HubConnection.ConnectionId, $"group-{index}",
                    TestContext.Current.CancellationToken);
            }
            scenario.Arm(caller.Token);
            operation = route switch
            {
                "connections" => manager.SendConnectionsAsync(
                    clients.Select(client => client.HubConnection.ConnectionId).ToArray(), "Hello", ["World"], caller.Token),
                "groups" => manager.SendGroupsAsync(["group-0", "group-1", "group-2"], "Hello", ["World"], caller.Token),
                "users" => manager.SendUsersAsync(["user-a", "user-b", "user-c"], "Hello", ["World"], caller.Token),
                _ => throw new ArgumentOutOfRangeException(nameof(route)),
            };

            await scenario.AdmissionFinished.Task.WaitAsync(Deadline, TestContext.Current.CancellationToken);
            foreach (Task entered in scenario.PipeEntries)
                await entered.WaitAsync(Deadline, TestContext.Current.CancellationToken);
            foreach (Task publish in scenario.ActualPublications)
                Assert.False(publish.IsCompleted);
            if (outcome == "canceled-prefix")
                caller.Cancel();
            scenario.ReleasePublications.TrySetResult();
            foreach (Task publish in scenario.ActualPublications)
                _ = await ObserveAsync(publish);

            if (outcome is "admission" or "null-task" or "success" or "async-fault")
            {
                int delivered = outcome == "success" ? 3 : outcome == "async-fault" ? 2 : 1;
                int[] indices = outcome == "async-fault" ? [0, 2] : Enumerable.Range(0, delivered).ToArray();
                foreach (int index in indices)
                {
                    InvocationMessage invocation = await clients[index].ReadInvocationAsync(Deadline, TestContext.Current.CancellationToken);
                    Assert.Equal("Hello", invocation.Target);
                    Assert.Equal("World", Assert.IsType<JsonElement>(Assert.Single(invocation.Arguments)).GetString());
                }
            }

            bool completedAtDisposeEntry = await scenario.DisposeEntered.Task.WaitAsync(Deadline, TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);
            scenario.ReleaseDisposal.TrySetResult();
            Exception? actual = await ObserveAsync(operation);
            Assert.True(completedAtDisposeEntry,
                "The real DI scope was released while an actual returned SDK publication was still pending.");
            Assert.Equal(1, scenario.DisposeCalls);
            Assert.Equal(outcome == "first-throw" ? 1 : outcome == "faults-cleanup" || outcome == "success"
                || outcome == "async-fault" ? 3 : 2, scenario.AdmissionCalls);
            AssertOutcome(scenario, actual);
        }
        catch (Exception exception)
        {
            primary = exception;
        }
        finally
        {
            // These independent joins make even the defective implementation terminate.
            // The immutable disposal-entry snapshot remains the product oracle.
            scenario.ReleasePublications.TrySetResult();
            scenario.ReleaseDisposal.TrySetResult();
            foreach (Task publish in scenario.ActualPublications)
                await CleanAsync(async () => { _ = await ObserveAsync(publish); }, cleanupFailures);
            if (operation is not null)
                await CleanAsync(async () => { _ = await ObserveAsync(operation); }, cleanupFailures);
            scenario.Disarm();
            foreach (HubConnectionTestClient client in clients)
            {
                if (manager is not null)
                    await CleanAsync(() => manager.OnDisconnectedAsync(client.HubConnection), cleanupFailures);
                await CleanAsync(() => client.DisposeAsync().AsTask(), cleanupFailures);
            }
            if (bus is not null)
                await CleanAsync(() => bus.StopAsync(CancellationToken.None), cleanupFailures);
            await CleanAsync(() => provider.DisposeAsync().AsTask(), cleanupFailures);
            LogContext.Current = previousLogContext!;
        }

        if (cleanupFailures.Count != 0)
            throw new AggregateException(primary is null ? cleanupFailures : [primary, .. cleanupFailures]);
        if (primary is not null)
            ExceptionDispatchInfo.Capture(primary).Throw();
    }

    private static void AssertOutcome(PublicationScenario scenario, Exception? actual)
    {
        switch (scenario.Outcome)
        {
            case "success":
                Assert.Null(actual);
                break;
            case "admission":
            case "first-throw":
            case "async-fault":
                Assert.Same(scenario.AdmissionFailure, actual);
                break;
            case "null-task":
                Assert.Equal("operations", Assert.IsType<ArgumentException>(actual).ParamName);
                break;
            case "canceled-prefix":
                Assert.Collection(Assert.IsType<AggregateException>(actual).InnerExceptions,
                    failure => Assert.Same(scenario.AdmissionFailure, failure),
                    failure => Assert.Equal(scenario.CallerToken,
                        Assert.IsAssignableFrom<OperationCanceledException>(failure).CancellationToken));
                Assert.True(Assert.Single(scenario.ActualPublications).IsCanceled);
                break;
            case "faults-cleanup":
                AggregateException combined = Assert.IsType<AggregateException>(actual);
                Assert.Collection(combined.InnerExceptions,
                    failure =>
                    {
                        var causes = Assert.IsType<AggregateException>(failure).InnerExceptions;
                        Assert.Equal(3, causes.Count);
                        Assert.Same(scenario.AdmissionFailure, causes[0]);
                        Assert.Contains(causes.Skip(1), cause => ReferenceEquals(scenario.FirstPublicationFailure, cause));
                        Assert.Contains(causes.Skip(1), cause => ReferenceEquals(scenario.NestedPublicationFailure, cause));
                    },
                    cleanup => Assert.Same(scenario.CleanupFailure, cleanup));
                Assert.Same(scenario.NestedLeaf, Assert.Single(scenario.NestedPublicationFailure.InnerExceptions));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario));
        }
    }

    private static async Task<Exception?> ObserveAsync(Task operation)
    {
        try
        {
            await operation.WaitAsync(Deadline);
            return null;
        }
        catch (Exception exception) when (exception is not TimeoutException)
        {
            return exception;
        }
    }

    private static async Task CleanAsync(Func<Task> cleanup, List<Exception> failures)
    {
        try { await cleanup().WaitAsync(Deadline); }
        catch (Exception exception) { failures.Add(exception); }
    }

    private sealed class PublicationScenario(string outcome)
    {
        public string Outcome { get; } = outcome;
        public Exception AdmissionFailure { get; } = new InvalidOperationException("Publication admission failed.");
        public Exception FirstPublicationFailure { get; } = new InvalidOperationException("First publication failed.");
        public Exception NestedLeaf { get; } = new InvalidOperationException("Nested publication cause.");
        public AggregateException NestedPublicationFailure { get; private set; } = null!;
        public Exception CleanupFailure { get; } = new InvalidOperationException("Real scoped collaborator cleanup failed.");
        public TaskCompletionSource ReleasePublications { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseDisposal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AdmissionFinished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> DisposeEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<Task> ActualPublications { get; } = [];
        public List<Task> PipeEntries { get; } = [];
        public CancellationToken CallerToken { get; private set; }
        public bool Armed { get; private set; }
        public int AdmissionCalls { get; private set; }
        public int DisposeCalls { get; private set; }

        public void Arm(CancellationToken token)
        {
            CallerToken = token;
            NestedPublicationFailure = new AggregateException(NestedLeaf);
            Armed = true;
        }

        public void Disarm() => Armed = false;

        public Task PublishAsync<T>(IPublishEndpoint endpoint, IServiceProvider provider, T message, CancellationToken token)
            where T : class
        {
            if (!Armed)
                return endpoint.PublishAsync(message, token);
            Assert.Equal(CallerToken, token);
            int index = AdmissionCalls++;
            bool stop = Outcome == "first-throw" || index == (Outcome == "faults-cleanup" ? 2 : 1)
                && Outcome is not "success" and not "async-fault";
            if (stop)
            {
                AdmissionFinished.TrySetResult();
                if (Outcome == "null-task")
                    return null!;
                throw AdmissionFailure;
            }
            if (Outcome == "async-fault" && index == 1)
                return Task.FromException(AdmissionFailure);
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            PipeEntries.Add(entered.Task);
            Task actual = endpoint.Advanced().PublishAsync(message, Pipe.ExecuteAwaited<PublishContext<T>>(async context =>
            {
                Assert.Equal(CallerToken, context.CancellationToken);
                Assert.Same(provider, context.GetPayload<IServiceProvider>());
                entered.TrySetResult();
                await ReleasePublications.Task.WaitAsync(Deadline);
                if (Outcome == "faults-cleanup")
                    throw index == 0 ? FirstPublicationFailure : NestedPublicationFailure;
                if (Outcome == "canceled-prefix")
                    throw new OperationCanceledException(CallerToken);
            }), token);
            ActualPublications.Add(actual);
            if (index == 2)
                AdmissionFinished.TrySetResult();
            return actual;
        }

        public async ValueTask DisposeAsync()
        {
            if (!Armed)
                return;
            DisposeCalls++;
            bool completed = ActualPublications.All(task => task.IsCompleted);
            DisposeEntered.TrySetResult(completed);
            await ReleaseDisposal.Task.WaitAsync(Deadline);
            if (Outcome == "faults-cleanup")
                throw CleanupFailure;
        }
    }

    private sealed class ObservedPublishEndpoint(
        IPublishEndpoint endpoint, IServiceProvider provider, PublicationScenario scenario) : IPublishEndpoint, IAsyncDisposable
    {
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class =>
            scenario.PublishAsync(endpoint, provider, message, cancellationToken);

        public Task PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken = default) where T : class =>
            endpoint.PublishAsync(message, options, cancellationToken);

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => endpoint.ConnectPublishObserver(observer);

        public ValueTask DisposeAsync() => scenario.DisposeAsync();
    }
}
