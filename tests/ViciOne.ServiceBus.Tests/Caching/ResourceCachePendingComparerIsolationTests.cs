using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class ResourceCachePendingComparerIsolationTests(ITestOutputHelper output)
{
    const string ChildCase = "VICIONE_CACHE_PENDING_CASE";
    const string ChildParent = "VICIONE_CACHE_PENDING_PARENT";
    const string ChildNonce = "VICIONE_CACHE_PENDING_NONCE";
    const string ChildDepth = "VICIONE_CACHE_PENDING_DEPTH";
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-FACTORY", "healthy-origin-pending-slot-joins-factory-request-and-cache-in-owned-process")]
    public Task HealthyOriginPendingSlotReleasesOwnership() =>
        RunAsync(nameof(HealthyOriginPendingSlotReleasesOwnership), _ => ExecuteCaseAsync(false));

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-FACTORY", "persistent-origin-comparer-failure-cannot-strand-pending-completion-or-capacity")]
    public Task PersistentOriginComparerFailureReleasesPendingOwnership() =>
        RunAsync(nameof(PersistentOriginComparerFailureReleasesPendingOwnership), _ => ExecuteCaseAsync(true));

    private Task RunAsync(string name, Func<string, Task> action)
    {
        string fullName = typeof(ResourceCachePendingComparerIsolationTests).FullName + "." + name;
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
        Console.WriteLine("CACHE_CASE_COMPLETED:" + Environment.GetEnvironmentVariable(ChildNonce) + ":" + fullName);
    }

    private async Task LaunchAsync(string fullName)
    {
        string assembly = typeof(ResourceCachePendingComparerIsolationTests).Assembly.Location;
        string apphost = Path.ChangeExtension(assembly, OperatingSystem.IsWindows() ? ".exe" : null);
        Assert.True(File.Exists(apphost));
        string nonce = Guid.NewGuid().ToString("N");
        string directory = Path.Combine(AppContext.BaseDirectory, "cache-pending-child-receipts", nonce);
        Directory.CreateDirectory(directory);
        var arguments = new[] { "--filter-class", typeof(ResourceCachePendingComparerIsolationTests).FullName!,
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
            Path.ChangeExtension(assembly, ".deps.json"), Path.ChangeExtension(assembly, ".runtimeconfig.json"), typeof(ResourceCache<>).Assembly.Location,
            typeof(IBus).Assembly.Location };
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
        output.WriteLine("Owned cache child receipt: " + directory);
        Assert.Null(waitFailure);
        Assert.Equal(before, after);
        Assert.True(process.ExitCode == 0, "The joined child failed; its actual finite cause is preserved in " + directory);
        Assert.Contains("CACHE_CASE_COMPLETED:" + nonce + ":" + fullName, actualOut, StringComparison.Ordinal);
        Match[] events = Regex.Matches(actualOut, @"^(?:erfolgreich|passed) (ViciOne\.ServiceBus\.Tests\.[^\r\n]*?) \([^\r\n]*?\)\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Cast<Match>().ToArray();
        Assert.Equal(fullName, Assert.Single(events).Groups[1].Value);
        Assert.Matches(@"(?im)^\s*(?:gesamt|total):\s*1\s*$", actualOut);
        Assert.Matches(@"(?im)^\s*(?:fehlerhaft|fehlgeschlagen|failed):\s*0\s*$", actualOut);
        Assert.Matches(@"(?im)^\s*(?:erfolgreich|passed|succeeded):\s*1\s*$", actualOut);
        Assert.Matches(@"(?im)^\s*(?:übersprungen|skipped):\s*0\s*$", actualOut);
    }

    private static string HashFile(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    async Task ExecuteCaseAsync(bool hostile)
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var cache = new ResourceCache<Owned>(new ResourceCacheOptions(capacity: 1, timeProvider: clock,
            cleanupInterval: TimeSpan.FromDays(1)));
        var operations = new List<Task>();
        var observedFailures = new Dictionary<Task, Exception>();
        var comparer = new Comparer(output);
        bool validatedStrand = false;
        Task? disposal = null;
        var candidate = new Owned("candidate");
        int factoryCalls = 0;
        int factoryReturns = 0;

        async Task<T> RunAsync<T>(Task<T> operation)
        {
            operations.Add(operation);
            try { return await operation.WaitAsync(Bound, TestContext.Current.CancellationToken); }
            catch (Exception failure) when (operation.IsCompleted && failure is not TimeoutException)
            { observedFailures[operation] = failure; throw; }
        }
        async Task ObserveAllAsync(int index)
        {
            if (index == operations.Count) return;
            Task operation = operations[index];
            try
            {
                try { await operation.WaitAsync(Bound, CancellationToken.None); }
                catch (Exception failure) when (operation.IsCompleted && failure is not TimeoutException
                    && observedFailures.TryGetValue(operation, out Exception? observed) && ReferenceEquals(observed, failure)) { }
            }
            finally { await ObserveAllAsync(index + 1); }
        }

        try
        {
            var index = cache.AddIndex("primary", x => x.Id, comparer: comparer);
            Task<Owned> request = index.GetOrAddAsync(candidate.Id, (_, _) =>
            {
                factoryCalls++;
                comparer.Armed = hostile;
                factoryReturns++;
                return ValueTask.FromResult(candidate);
            }, TestContext.Current.CancellationToken).AsTask();
            operations.Add(request);
            Exception? captured = null;
            if (request.IsCompleted)
            {
                // Observe an already terminal source outcome before assertions, so cleanup
                // cannot replace a later physical-ownership failure with that known outcome.
                try { await request.WaitAsync(Bound, TestContext.Current.CancellationToken); }
                catch (Exception failure) when (request.IsCompleted && failure is not TimeoutException)
                { observedFailures[request] = failure; captured = failure; }
            }
            // Only this fully validated synchronous corruption signature permits child isolation
            // to replace an impossible original product join. All other paths retain owner cleanup.
            int pending = cache.Statistics.PendingCreations;
            output.WriteLine($"STATE factoryCalls={factoryCalls} factoryReturns={factoryReturns} release={candidate.DisposeCalls} pending={pending} requestTerminal={request.IsCompleted} comparerThrows={comparer.Throws}");
            Assert.Equal(1, factoryCalls);
            Assert.Equal(1, factoryReturns);
            Assert.Equal(hostile ? 1 : 0, candidate.DisposeCalls);
            if (hostile && pending == 1 && comparer.Throws >= 2 && !request.IsCompleted)
            {
                validatedStrand = true;
                output.WriteLine("VALIDATED_STRANDED_PENDING: finite state failure before any request await; only this original/mutant child route has no product request/slot/cache join credit.");
                Assert.Equal(0, pending);
            }
            Assert.Equal(0, pending);
            Assert.True(request.IsCompleted);
            comparer.Armed = false;
            Assert.Equal(hostile ? 0 : 1, cache.Statistics.Count);
            Assert.Equal(hostile ? 1 : 0, cache.Statistics.CreationFaults);
            if (hostile)
            {
                Assert.Empty(cache.GetValues(TestContext.Current.CancellationToken));
                Task<Owned> lookup = index.GetAsync(candidate.Id, TestContext.Current.CancellationToken).AsTask();
                Assert.IsType<KeyNotFoundException>(await Record.ExceptionAsync(() => RunAsync(lookup)));
                Assert.Same(comparer.Failure, captured);
            }
            else
            {
                Assert.Null(captured);
                Assert.Same(candidate, request.Result);
                Assert.Same(candidate, Assert.Single(cache.GetValues(TestContext.Current.CancellationToken)));
                Assert.True(await RunAsync(index.RemoveAsync(candidate.Id, TestContext.Current.CancellationToken).AsTask()));
                Assert.Equal(1, candidate.DisposeCalls);
            }
            var replacement = new Owned("replacement");
            Assert.Same(replacement, await RunAsync(index.GetOrAddAsync(replacement.Id,
                (_, _) => ValueTask.FromResult(replacement), TestContext.Current.CancellationToken).AsTask()));
            Assert.Equal(0, cache.Statistics.PendingCreations);
            disposal = cache.DisposeAsync().AsTask();
            await disposal.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(1, candidate.DisposeCalls);
            Assert.Equal(1, replacement.DisposeCalls);
        }
        finally
        {
            comparer.Armed = false;
            if (!validatedStrand)
            {
                try { await ObserveAllAsync(0); }
                finally
                {
                    disposal ??= cache.DisposeAsync().AsTask();
                    await disposal.WaitAsync(Bound, CancellationToken.None);
                }
            }
        }
    }

    sealed class Comparer(ITestOutputHelper output) : IEqualityComparer<string>
    {
        public bool Armed { get; set; }
        public int Throws { get; private set; }
        public Exception Failure { get; } = new InvalidOperationException("chosen persistent origin comparer failure");
        public int GetHashCode(string key)
        {
            if (Armed)
            {
                Throws++;
                output.WriteLine("ACTUAL_COMPARER_ATTEMPT: " + key + "\n" + new StackTrace());
                throw Failure;
            }
            return StringComparer.Ordinal.GetHashCode(key);
        }
        public bool Equals(string? left, string? right) => StringComparer.Ordinal.Equals(left, right);
    }

    sealed class Owned(string id) : IAsyncDisposable
    {
        public string Id { get; } = id;
        public int DisposeCalls { get; private set; }
        public ValueTask DisposeAsync() { DisposeCalls++; return ValueTask.CompletedTask; }
    }
}
