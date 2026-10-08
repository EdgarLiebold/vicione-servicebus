using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class ColdTelemetrySourceIsolationTests(ITestOutputHelper output)
{
    private const string ChildCase = "VICIONE_COLD_TELEMETRY_CASE";
    private const string ChildParent = "VICIONE_COLD_TELEMETRY_PARENT";
    private const string ChildNonce = "VICIONE_COLD_TELEMETRY_NONCE";
    private const string ChildDepth = "VICIONE_COLD_TELEMETRY_DEPTH";
    private static readonly DateTimeOffset Epoch = new(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);

    [Fact, MethodImpl(MethodImplOptions.NoInlining)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "cold-optional-timeline-healthy-public-activation")]
    public Task OptionalTimeline_ColdHealthyPublicActivationWorksInFreshProcess() =>
        RunAsync(nameof(OptionalTimeline_ColdHealthyPublicActivationWorksInFreshProcess), name => OptionalAsync(name, false));

    [Fact, MethodImpl(MethodImplOptions.NoInlining)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "cold-optional-timeline-source-failure-stays-inert")]
    public Task OptionalTimeline_ColdSourceFailureRemainsInertAcrossPublicActivations() =>
        RunAsync(nameof(OptionalTimeline_ColdSourceFailureRemainsInertAcrossPublicActivations), name => OptionalAsync(name, true));

    [Fact, MethodImpl(MethodImplOptions.NoInlining)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "cold-required-monitor-healthy-exact-idle-deadline")]
    public Task RequiredMonitor_ColdHealthyPublicActionObeysExactIdleDeadline() =>
        RunAsync(nameof(RequiredMonitor_ColdHealthyPublicActionObeysExactIdleDeadline), _ => RequiredAsync(false));

    [Fact, MethodImpl(MethodImplOptions.NoInlining)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "cold-required-monitor-source-primary-and-cache")]
    public Task RequiredMonitor_ColdSourceFailurePreservesOriginalAndCachesOneAttempt() =>
        RunAsync(nameof(RequiredMonitor_ColdSourceFailurePreservesOriginalAndCachesOneAttempt), _ => RequiredAsync(true));

    [Fact, MethodImpl(MethodImplOptions.NoInlining)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "required-monitor-constructor-unwind-keeps-primary")]
    public Task RequiredMonitor_StartFailureKeepsPrimaryWhenOwnedTimerDisposeThrows() =>
        RunAsync(nameof(RequiredMonitor_StartFailureKeepsPrimaryWhenOwnedTimerDisposeThrows), _ => UnwindAsync());

    [Fact, MethodImpl(MethodImplOptions.NoInlining)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "required-monitor-standalone-cleanup-remains-required")]
    public Task RequiredMonitor_StandaloneTimerCleanupFailureRemainsRequired() =>
        RunAsync(nameof(RequiredMonitor_StandaloneTimerCleanupFailureRemainsRequired), _ => RequiredAsync(false, true));

    [Fact, MethodImpl(MethodImplOptions.NoInlining)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "cold-required-monitor-nonthrowing-constructor-restores-parent")]
    public Task RequiredMonitor_NonthrowingSourceObserverCannotChangeRootParent() =>
        RunAsync(nameof(RequiredMonitor_NonthrowingSourceObserverCannotChangeRootParent), _ => RequiredAsync(false, false, true));

    private Task RunAsync(string name, Func<string, Task> action)
    {
        string fullName = typeof(ColdTelemetrySourceIsolationTests).FullName + "." + name;
        string? selected = Environment.GetEnvironmentVariable(ChildCase);
        if (selected is null) return LaunchAsync(fullName);
        Assert.Equal(fullName, selected);
        Assert.Equal("1", Environment.GetEnvironmentVariable(ChildDepth));
        Assert.True(int.TryParse(Environment.GetEnvironmentVariable(ChildParent), out int parent) && parent > 0);
        Assert.NotEqual(parent, Environment.ProcessId);
        Assert.True(Guid.TryParseExact(Environment.GetEnvironmentVariable(ChildNonce), "N", out _));
        return RunChildAsync(name, fullName, action);
    }

    private static async Task RunChildAsync(string name, string fullName, Func<string, Task> action)
    {
        await action(name);
        Console.WriteLine("COLD_CASE_COMPLETED:" + Environment.GetEnvironmentVariable(ChildNonce) + ":" + fullName);
    }

    private async Task LaunchAsync(string fullName)
    {
        string assembly = typeof(ColdTelemetrySourceIsolationTests).Assembly.Location;
        string apphost = Path.ChangeExtension(assembly, OperatingSystem.IsWindows() ? ".exe" : null);
        Assert.True(File.Exists(apphost));
        string nonce = Guid.NewGuid().ToString("N");
        string directory = Path.Combine(AppContext.BaseDirectory, "cold-child-receipts", nonce);
        Directory.CreateDirectory(directory);
        var arguments = new[] { "--filter-class", typeof(ColdTelemetrySourceIsolationTests).FullName!,
            "--filter-method", fullName, "--minimum-expected-tests", "1", "--output", "Detailed",
            "--progress", "off", "--no-ansi", "--show-stdout", "All", "--show-stderr", "All",
            "--exit-on-process-exit", Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--results-directory", Path.Combine(directory, "native-results") };
        var start = new ProcessStartInfo(apphost) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        start.Environment[ChildCase] = fullName;
        start.Environment[ChildParent] = Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        start.Environment[ChildNonce] = nonce;
        start.Environment[ChildDepth] = "1";
        var runtime = new[] { apphost, assembly, Path.ChangeExtension(assembly, ".pdb"),
            Path.ChangeExtension(assembly, ".deps.json"), Path.ChangeExtension(assembly, ".runtimeconfig.json"), typeof(TelemetryActivityExtensions).Assembly.Location,
            Path.Combine(AppContext.BaseDirectory, "ViciOne.ServiceBus.dll") };
        var before = runtime.ToDictionary(path => Path.GetFileName(path)!, HashFile);
        using var process = new Process { StartInfo = start };
        Assert.True(process.Start());
        int childPid = process.Id;
        Assert.NotEqual(Environment.ProcessId, childPid);
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        Task<string> stderr = process.StandardError.ReadToEndAsync(CancellationToken.None);
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        bound.CancelAfter(TimeSpan.FromSeconds(45));
        Exception? waitFailure = null;
        try { await process.WaitForExitAsync(bound.Token); }
        catch (Exception error) { waitFailure = error; }
        finally
        {
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) when (process.HasExited) { }
            }
            await process.WaitForExitAsync(CancellationToken.None);
        }
        string actualOut = await stdout;
        string actualErr = await stderr;
        await File.WriteAllTextAsync(Path.Combine(directory, "stdout.log"), actualOut, CancellationToken.None);
        await File.WriteAllTextAsync(Path.Combine(directory, "stderr.log"), actualErr, CancellationToken.None);
        var after = runtime.ToDictionary(path => Path.GetFileName(path)!, HashFile);
        var receipt = new { fullName, nonce, parentPid = Environment.ProcessId, childPid, apphost, arguments,
            exitCode = process.ExitCode, waitFailure = waitFailure?.GetType().FullName, before, after };
        await File.WriteAllTextAsync(Path.Combine(directory, "PROCESS.json"), JsonSerializer.Serialize(receipt), CancellationToken.None);
        output.WriteLine("Owned cold child receipt: " + directory);
        Assert.Null(waitFailure);
        Assert.Equal(before, after);
        Assert.True(process.ExitCode == 0, "The joined child failed; its actual finite cause is preserved in " + directory);
        Assert.Contains("COLD_CASE_COMPLETED:" + nonce + ":" + fullName, actualOut, StringComparison.Ordinal);
        Match[] events = Regex.Matches(actualOut, @"^(?:erfolgreich|passed) (ViciOne\.ServiceBus\.Tests\.[^\r\n]*?) \([^\r\n]*?\)\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Cast<Match>().ToArray();
        Assert.Equal(fullName, Assert.Single(events).Groups[1].Value);
        Assert.Matches(@"(?im)^\s*(?:gesamt|total):\s*1\s*$", actualOut);
        Assert.Matches(@"(?im)^\s*(?:fehlerhaft|fehlgeschlagen|failed):\s*0\s*$", actualOut);
        Assert.Matches(@"(?im)^\s*(?:erfolgreich|passed|succeeded):\s*1\s*$", actualOut);
        Assert.Matches(@"(?im)^\s*(?:übersprungen|skipped):\s*0\s*$", actualOut);
    }

    private static string HashFile(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static async Task OptionalAsync(string name, bool hostile)
    {
        using var caller = new Activity("cold-optional-caller").Start();
        Assert.NotNull(caller);
        using var gate = new SourceGate("ViciOne.ServiceBus.TestHarness", hostile);
        using var probe = new ActivitySource("cold-optional-unrelated");
        Assert.Equal(0, gate.Constructions);
        Assert.Null(probe.StartActivity("before-arm"));
        gate.Armed = true;
        using var firstWriter = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        using var secondWriter = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        // Capture both Fact method names synchronously, before any await can remove the Fact's stack frame.
        ServiceProvider[] providers = [CreateProvider(firstWriter), CreateProvider(secondWriter)];
        TextWriter[] writers = [firstWriter, secondWriter];
        int actions = 0;
        try
        {
            for (int index = 0; index < providers.Length; index++)
            {
                ITestHarness? harness = null;
                Exception? escaped = Record.Exception(() => harness = providers[index].GetTestHarness());
                Assert.Equal(1, gate.Constructions);
                Assert.Null(escaped);
                Assert.NotNull(harness);
                TaskCompletionSource<int> actual = harness.CreateTaskCompletionSource<int>(TestContext.Current.CancellationToken);
                actions++;
                actual.SetResult(41);
                Assert.Equal(41, await actual.Task);
                if (hostile)
                {
                    Assert.Same(caller, Activity.Current);
                    Assert.Null(probe.StartActivity("failed-timeline-has-no-sampler"));
                }
                else
                {
                    Activity root = Assert.IsType<Activity>(Activity.Current);
                    Assert.Equal("ViciOne.ServiceBus.TestHarness", root.Source.Name);
                    Assert.Equal(name, root.OperationName);
                    Assert.Equal(caller.Id, root.ParentId);
                    Activity.Current = null;
                    using (Activity? work = probe.StartActivity("cold-timeline-work-" + index))
                        Assert.NotNull(work);
                    Activity.Current = root;
                }
                await providers[index].DisposeAsync();
                Assert.Same(caller, Activity.Current);
                if (!hostile) Assert.Contains("cold-timeline-work-" + index, writers[index].ToString(), StringComparison.Ordinal);
                gate.Throw = false; // Keep the construction counter registered across the second activation.
            }
            Assert.Equal(2, actions);
            Assert.Equal(1, gate.Constructions);
            Assert.Null(probe.StartActivity("after-all-disposals"));
        }
        finally
        {
            gate.Throw = false;
            foreach (ServiceProvider provider in providers) await provider.DisposeAsync();
            Activity.Current = caller;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ServiceProvider CreateProvider(TextWriter writer)
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTestHarness(writer);
        services.AddViciOneServiceBusTestTelemetry(writer);
        return services.BuildServiceProvider();
    }

    private static async Task RequiredAsync(bool hostile, bool failCleanup = false, bool mutateConstruction = false)
    {
        using var caller = new Activity("cold-required-caller").Start();
        Assert.NotNull(caller);
        using var gate = new SourceGate("ViciOne.ServiceBus.Testing.Monitor", hostile);
        using var probe = new ActivitySource("cold-required-unrelated");
        Assert.Equal(0, gate.Constructions);
        Assert.Null(probe.StartActivity("before-arm"));
        gate.Armed = true;
        Exception? cleanupFailure = failCleanup ? new InvalidOperationException("required standalone timer cleanup failed") : null;
        gate.MutateCurrentOnConstruction = mutateConstruction;
        var clock = new ObservableTimeProvider(Epoch, timerDisposeException: cleanupFailure);
        IPublishEndpoint endpoint = DispatchProxy.Create<IPublishEndpoint, RejectPublishProxy>();
        var proxy = (RejectPublishProxy)endpoint;
        int actions = 0;
        Activity? root = null;
        async Task OperationAsync()
        {
            await endpoint.ExecuteAndWaitForIdleAsync(actual =>
            {
                Assert.Same(endpoint, actual);
                actions++;
                root = Activity.Current;
                Assert.NotNull(root);
                Assert.Equal("ViciOne.ServiceBus.Testing.Monitor", root.Source.Name);
                Assert.Equal(caller.Id, root.ParentId);
                return Task.CompletedTask;
            }, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1), clock, TestContext.Current.CancellationToken);
        }
        Task? operation = null;
        try
        {
            for (int attempt = 0; attempt < (hostile ? 2 : 1); attempt++)
            {
                operation = OperationAsync();
                Assert.Equal(1, gate.Constructions);
                if (!hostile && operation.IsFaulted) await operation; // Preserve the actual action/root assertion before timer controls.
                if (hostile)
                {
                    Assert.Equal(0, actions);
                    Assert.Equal(0, proxy.Calls);
                    Assert.Equal(0, clock.TimerCount);
                    Assert.Equal(0, clock.ActiveTimerCount);
                    Assert.True(operation.IsCompleted);
                    Exception? escaped = await Record.ExceptionAsync(async () => await operation);
                    Assert.Same(gate.Failure, escaped);
                    Assert.Same(caller, Activity.Current);
                    Assert.Null(probe.StartActivity("failed-monitor-has-no-sampler"));
                    gate.Throw = false;
                }
                else
                {
                    Assert.Equal(1, actions);
                    Assert.Equal(0, proxy.Calls);
                    Assert.Equal(1, clock.TimerCount);
                    Assert.Equal(TimeSpan.FromMinutes(1), clock.LastDueTime);
                    clock.Advance(TimeSpan.FromMinutes(1) - TimeSpan.FromTicks(1));
                    Assert.False(operation.IsCompleted);
                    clock.Advance(TimeSpan.FromTicks(1));
                    if (cleanupFailure is null)
                        await operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                    else
                    {
                        Exception? escaped = await Record.ExceptionAsync(async () =>
                            await operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
                        Assert.Same(cleanupFailure, escaped);
                    }
                    Assert.Equal(0, clock.ActiveTimerCount);
                    Assert.True(Assert.IsType<Activity>(root).IsStopped);
                    Assert.Same(caller, Activity.Current);
                    Assert.Null(probe.StartActivity("after-monitor-disposal"));
                }
            }
        }
        finally
        {
            gate.Throw = false;
            if (operation is { IsCompleted: false })
            {
                clock.Advance(TimeSpan.FromMinutes(10));
                await Record.ExceptionAsync(async () => await operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            }
            Activity.Current = caller;
        }
    }

    private static async Task UnwindAsync()
    {
        using var caller = new Activity("cold-unwind-caller").Start();
        Assert.NotNull(caller);
        using var gate = new SourceGate("ViciOne.ServiceBus.Testing.Monitor", false, true);
        using var workSource = new ActivitySource("cold-unwind-child");
        using var probe = new ActivitySource("cold-unwind-unrelated");
        Assert.Equal(0, gate.Constructions);
        Assert.Null(probe.StartActivity("before-arm"));
        var primary = new InvalidOperationException("required monitor start failed");
        var secondary = new InvalidOperationException("owned timer cleanup failed");
        var clock = new ObservableTimeProvider(Epoch, timerDisposeException: secondary);
        Activity? root = null;
        Activity? child = null;
        int actions = 0;
        gate.Started = activity =>
        {
            root = activity;
            child = workSource.StartActivity("actual-related-child");
            Assert.NotNull(child);
            Assert.Equal(root.RootId, child.RootId);
            Assert.Equal(1, clock.TimerCount);
            throw primary;
        };
        gate.Armed = true;
        IPublishEndpoint endpoint = DispatchProxy.Create<IPublishEndpoint, RejectPublishProxy>();
        var proxy = (RejectPublishProxy)endpoint;
        try
        {
            Exception? escaped = await Record.ExceptionAsync(async () => await endpoint.ExecuteAndWaitForIdleAsync(_ =>
            { actions++; return Task.CompletedTask; }, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1), clock, TestContext.Current.CancellationToken));
            Assert.Equal(1, gate.Constructions);
            Assert.Equal(0, actions);
            Assert.Equal(0, proxy.Calls);
            Assert.Equal(1, clock.TimerCount);
            Assert.Equal(0, clock.ActiveTimerCount);
            Assert.True(Assert.IsType<Activity>(root).IsStopped);
            Assert.Same(primary, escaped);
            Assert.Same(caller, Activity.Current);
            Assert.Null(probe.StartActivity("after-constructor-unwind"));
        }
        finally
        {
            gate.Started = null;
            child?.Dispose();
            Activity.Current = caller;
        }
    }

    private sealed class SourceGate : IDisposable
    {
        private readonly ActivityListener _listener;
        public int Constructions { get; private set; }
        public bool Armed { get; set; }
        public bool Throw { get; set; }
        public bool MutateCurrentOnConstruction { get; set; }
        public Action<Activity>? Started { get; set; }
        public Exception Failure { get; } = new InvalidOperationException("foreign cold source constructor failed");
        public SourceGate(string target, bool hostile, bool observeStarted = false)
        {
            Throw = hostile;
            _listener = new ActivityListener
            {
                ShouldListenTo = source =>
                {
                    if (source.Name != target) return false;
                    Constructions++;
                    if (Armed && MutateCurrentOnConstruction) Activity.Current = null;
                    if (Armed && Throw) { Activity.Current = null; throw Failure; }
                    return observeStarted;
                },
                Sample = (ref ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity => Started?.Invoke(activity),
            };
            ActivitySource.AddActivityListener(_listener);
        }
        public void Dispose() => _listener.Dispose();
    }

    public class RejectPublishProxy : DispatchProxy
    {
        public int Calls { get; private set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls++;
            throw new InvalidOperationException("The harmless test action must not invoke the endpoint.");
        }
    }
}
