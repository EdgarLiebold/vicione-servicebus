using System.Diagnostics;
using System.Text;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Tooling;
/// <summary>Black-box contracts for the retained provider-only orchestration boundary.</summary>
public sealed class ProviderFixtureRunnerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [RequirementCoverage("REQ-TEST-203", "provider-runner-preserves-native-mtp-exit-code-and-cleans-fixture")]
    public async Task CommandMode_PreservesChildExitCodeAndCleansTheFixtureAsync(int childExitCode)
    {
        await using RunnerFixture fixture = RunnerFixture.Create();

        ProcessResult result = await fixture.RunAsync(
            childExitCode,
            loopbackHost: "127.0.0.1",
            failFinalTeardown: false);

        Assert.Equal(childExitCode, result.ExitCode);
        Assert.Contains("fixture azurite ready on loopback", result.StandardOutput, StringComparison.Ordinal);
        Assert.Equal(2, fixture.DockerCalls.Count(call => call.Contains(" down -v --remove-orphans", StringComparison.Ordinal)));
        Assert.Single(fixture.DockerCalls, call => call.Contains(" up -d --wait azurite", StringComparison.Ordinal));
        Assert.Contains(fixture.DockerCalls, call => call.Contains(" port azurite 10000", StringComparison.Ordinal));
        Assert.Contains(fixture.DockerCalls, call => call.Contains(" port azurite 10002", StringComparison.Ordinal));
        Assert.Contains(fixture.DockerCalls, call => call.Contains(" logs --no-color --timestamps azurite", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [RequirementCoverage("REQ-TEST-203", "provider-runner-category-mode-uses-native-mtp-trait-and-preserves-exit-code")]
    public async Task CategoryMode_UsesNativeMtpTraitAndPreservesExitCodeAsync(int childExitCode)
    {
        await using RunnerFixture fixture = RunnerFixture.Create();

        ProcessResult result = await fixture.RunCategoryAsync(childExitCode);

        Assert.Equal(childExitCode, result.ExitCode);
        string invocation = Assert.Single(fixture.DotnetCalls);
        Assert.Contains("test --project", invocation, StringComparison.Ordinal);
        Assert.Contains("ViciOne.ServiceBus.Architecture.Tests.csproj", invocation, StringComparison.Ordinal);
        Assert.Contains("--filter-trait Category=Architecture", invocation, StringComparison.Ordinal);
        Assert.Contains("--minimum-expected-tests 1", invocation, StringComparison.Ordinal);
        Assert.Contains("--results-directory", invocation, StringComparison.Ordinal);
        Assert.Equal("UTC", fixture.DotnetTimeZone);
        Assert.Equal(2, fixture.DockerCalls.Count(call => call.Contains(" down -v --remove-orphans", StringComparison.Ordinal)));
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-203", "provider-runner-refuses-non-loopback-publication-before-child")]
    public async Task CommandMode_RefusesANonLoopbackProviderEndpointBeforeRunningTheChildAsync()
    {
        await using RunnerFixture fixture = RunnerFixture.Create();

        ProcessResult result = await fixture.RunAsync(
            0,
            loopbackHost: "0.0.0.0",
            failFinalTeardown: false,
            createChildMarker: true);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("instead of loopback", result.StandardError, StringComparison.Ordinal);
        Assert.False(File.Exists(fixture.ChildMarker));
        Assert.Equal(2, fixture.DockerCalls.Count(call => call.Contains(" down -v --remove-orphans", StringComparison.Ordinal)));
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-203", "provider-runner-makes-cleanup-failure-terminal")]
    public async Task CommandMode_MakesCleanupFailureRedWhenTheChildPassedAsync()
    {
        await using RunnerFixture fixture = RunnerFixture.Create();

        ProcessResult result = await fixture.RunAsync(
            0,
            loopbackHost: "127.0.0.1",
            failFinalTeardown: true);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("the fixture could not be returned to a usable state", result.StandardError, StringComparison.Ordinal);
        Assert.Equal(2, fixture.DockerCalls.Count(call => call.Contains(" down -v --remove-orphans", StringComparison.Ordinal)));
    }

    [Fact]
    [RequirementCoverage("REQ-TEST-203", "provider-runner-refuses-output-root-outside-owned-area")]
    public async Task CommandMode_RefusesAHandedRunRootOutsideTheRepositoryOwnedAreaAsync()
    {
        await using RunnerFixture fixture = RunnerFixture.Create(runRootOutsideOwnedArea: true);

        ProcessResult result = await fixture.RunAsync(
            0,
            loopbackHost: "127.0.0.1",
            failFinalTeardown: false);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("is not a direct child of the run area", result.StandardError, StringComparison.Ordinal);
        Assert.Empty(fixture.DockerCalls);
    }

    private sealed class RunnerFixture : IAsyncDisposable
    {
        private readonly string _temporaryDirectory;
        private readonly string _runRoot;
        private readonly string _token;
        private readonly string _dockerLog;
        private readonly string _dotnetLog;
        private readonly string _dotnetTimeZone;
        private readonly string _downState;

        private RunnerFixture(
            string temporaryDirectory,
            string runRoot,
            string token,
            string dockerLog,
            string dotnetLog,
            string dotnetTimeZone,
            string downState)
        {
            _temporaryDirectory = temporaryDirectory;
            _runRoot = runRoot;
            _token = token;
            _dockerLog = dockerLog;
            _dotnetLog = dotnetLog;
            _dotnetTimeZone = dotnetTimeZone;
            _downState = downState;
            ChildMarker = Path.Combine(temporaryDirectory, "child-ran");
        }

        internal string ChildMarker { get; }

        internal IReadOnlyList<string> DockerCalls => File.Exists(_dockerLog)
            ? File.ReadAllLines(_dockerLog)
            : [];

        internal IReadOnlyList<string> DotnetCalls => File.Exists(_dotnetLog)
            ? File.ReadAllLines(_dotnetLog)
            : [];

        internal string? DotnetTimeZone => File.Exists(_dotnetTimeZone)
            ? File.ReadAllText(_dotnetTimeZone).Trim()
            : null;

        internal static RunnerFixture Create(bool runRootOutsideOwnedArea = false)
        {
            if (OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException(
                    "The provider fixture runner is a Linux-only repository tool.");
            }

            string temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                $"vicione-provider-runner-{Guid.NewGuid():N}");
            Directory.CreateDirectory(temporaryDirectory);

            string runName = $"vicione-{Guid.NewGuid():N}"[..20];
            string ownedArea = Path.Combine(RepositoryLayout.Root, "artifacts", "run-output");
            string runRoot = runRootOutsideOwnedArea
                ? Path.Combine(temporaryDirectory, runName)
                : Path.Combine(ownedArea, runName);
            Directory.CreateDirectory(runRoot);
            string token = Convert.ToHexStringLower(Guid.NewGuid().ToByteArray());
            File.WriteAllText(
                Path.Combine(runRoot, "run-root.token"),
                token + "\n",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            string docker = Path.Combine(temporaryDirectory, "docker");
            File.WriteAllText(docker, FakeDockerSource, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.SetUnixFileMode(
                docker,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            string dotnet = Path.Combine(temporaryDirectory, "dotnet");
            File.WriteAllText(dotnet, FakeDotnetSource, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.SetUnixFileMode(
                dotnet,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            return new RunnerFixture(
                temporaryDirectory,
                runRoot,
                token,
                Path.Combine(temporaryDirectory, "docker-calls.log"),
                Path.Combine(temporaryDirectory, "dotnet-calls.log"),
                Path.Combine(temporaryDirectory, "dotnet-time-zone.log"),
                Path.Combine(temporaryDirectory, "down-state"));
        }

        internal async Task<ProcessResult> RunAsync(
            int childExitCode,
            string loopbackHost,
            bool failFinalTeardown,
            bool createChildMarker = false)
        {
            string runner = Path.Combine(RepositoryLayout.Root, "tools", "ci", "run_broker_category.py");
            var start = new ProcessStartInfo
            {
                FileName = "python3",
                WorkingDirectory = RepositoryLayout.Root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            start.ArgumentList.Add(runner);
            start.ArgumentList.Add("--broker");
            start.ArgumentList.Add("azurite");
            start.ArgumentList.Add("--command");
            start.ArgumentList.Add("--");
            start.ArgumentList.Add("/bin/sh");
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add(createChildMarker
                ? $"printf child > '{ChildMarker.Replace("'", "'\\''", StringComparison.Ordinal)}'"
                : $"exit {childExitCode}");

            start.Environment["PATH"] = _temporaryDirectory + Path.PathSeparator + start.Environment["PATH"];
            start.Environment["VICIONE_SERVICEBUS_RUN_ROOT"] = _runRoot;
            start.Environment["VICIONE_SERVICEBUS_RUN_TOKEN"] = _token;
            start.Environment["VICIONE_FAKE_DOCKER_LOG"] = _dockerLog;
            start.Environment["VICIONE_FAKE_DOCKER_DOWN_STATE"] = _downState;
            start.Environment["VICIONE_FAKE_DOCKER_HOST"] = loopbackHost;
            start.Environment["VICIONE_FAKE_DOCKER_FAIL_FINAL_DOWN"] = failFinalTeardown ? "1" : "0";
            start.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
            start.Environment["PYTHONPYCACHEPREFIX"] = Path.Combine(_temporaryDirectory, "pycache");
            start.Environment.Remove("GITHUB_ACTIONS");

            using var process = new Process { StartInfo = start };
            Assert.True(process.Start());
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            Task<string> standardError = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
            await process.WaitForExitAsync(TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

            return new ProcessResult(process.ExitCode, await standardOutput, await standardError);
        }

        internal async Task<ProcessResult> RunCategoryAsync(int childExitCode)
        {
            string runner = Path.Combine(RepositoryLayout.Root, "tools", "ci", "run_broker_category.py");
            var start = new ProcessStartInfo
            {
                FileName = "python3",
                WorkingDirectory = RepositoryLayout.Root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            start.ArgumentList.Add(runner);
            start.ArgumentList.Add("--broker");
            start.ArgumentList.Add("azurite");
            start.ArgumentList.Add("--category");
            start.ArgumentList.Add("Architecture");
            start.ArgumentList.Add("--project");
            start.ArgumentList.Add("tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/ViciOne.ServiceBus.Architecture.Tests.csproj");

            start.Environment["PATH"] = _temporaryDirectory + Path.PathSeparator + start.Environment["PATH"];
            start.Environment["VICIONE_SERVICEBUS_RUN_ROOT"] = _runRoot;
            start.Environment["VICIONE_SERVICEBUS_RUN_TOKEN"] = _token;
            start.Environment["VICIONE_FAKE_DOCKER_LOG"] = _dockerLog;
            start.Environment["VICIONE_FAKE_DOCKER_DOWN_STATE"] = _downState;
            start.Environment["VICIONE_FAKE_DOCKER_HOST"] = "127.0.0.1";
            start.Environment["VICIONE_FAKE_DOCKER_FAIL_FINAL_DOWN"] = "0";
            start.Environment["VICIONE_FAKE_DOTNET_LOG"] = _dotnetLog;
            start.Environment["VICIONE_FAKE_DOTNET_TZ"] = _dotnetTimeZone;
            start.Environment["VICIONE_FAKE_DOTNET_EXIT"] = childExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
            start.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
            start.Environment["PYTHONPYCACHEPREFIX"] = Path.Combine(_temporaryDirectory, "pycache");
            start.Environment.Remove("GITHUB_ACTIONS");

            using var process = new Process { StartInfo = start };
            Assert.True(process.Start());
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            Task<string> standardError = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
            await process.WaitForExitAsync(TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

            return new ProcessResult(process.ExitCode, await standardOutput, await standardError);
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(_runRoot))
                Directory.Delete(_runRoot, recursive: true);
            if (Directory.Exists(_temporaryDirectory))
                Directory.Delete(_temporaryDirectory, recursive: true);

            return ValueTask.CompletedTask;
        }

        private const string FakeDockerSource = """
            #!/bin/sh
            printf '%s\n' "$*" >> "$VICIONE_FAKE_DOCKER_LOG"
            case " $* " in
              *" down -v --remove-orphans "*)
                down_count=0
                if [ -f "$VICIONE_FAKE_DOCKER_DOWN_STATE" ]; then
                  read down_count < "$VICIONE_FAKE_DOCKER_DOWN_STATE"
                fi
                down_count=$((down_count + 1))
                printf '%s\n' "$down_count" > "$VICIONE_FAKE_DOCKER_DOWN_STATE"
                if [ "$VICIONE_FAKE_DOCKER_FAIL_FINAL_DOWN" = "1" ] && [ "$down_count" -ge 2 ]; then
                  printf '%s\n' 'controlled teardown failure' >&2
                  exit 42
                fi
                ;;
              *" port azurite 10000 "*)
                printf '%s:%s\n' "$VICIONE_FAKE_DOCKER_HOST" '43100'
                ;;
              *" port azurite 10002 "*)
                printf '%s:%s\n' "$VICIONE_FAKE_DOCKER_HOST" '43102'
                ;;
              *" logs --no-color --timestamps azurite "*)
                printf '%s\n' 'controlled provider log'
                ;;
            esac
            exit 0
            """;

        private const string FakeDotnetSource = """
            #!/bin/sh
            printf '%s\n' "$*" >> "$VICIONE_FAKE_DOTNET_LOG"
            printf '%s\n' "${TZ-}" > "$VICIONE_FAKE_DOTNET_TZ"
            exit "$VICIONE_FAKE_DOTNET_EXIT"
            """;
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
