using System.Runtime.CompilerServices;
using System.Text.Json;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Diagnostics.Tests;

public sealed class ResultDeliveryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-DELIVERY", "validation-failure-to-requested-file")]
    public async Task ValidationFailureReachesRequestedFile()
    {
        using var sink = new TemporaryFile();

        int code = await Program.Main(["publish-load", "--messages", "0", "--output", sink.Path]);

        Assert.Equal(1, code);
        Assert.True(File.Exists(sink.Path));
        using JsonDocument written = JsonDocument.Parse(
            await File.ReadAllTextAsync(sink.Path, TestContext.Current.CancellationToken));
        Assert.Equal("failed", written.RootElement.GetProperty("status").GetString());
        Assert.Contains("--messages", written.RootElement.GetProperty("error").GetString(),
            StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-DELIVERY", "unwritable-sink-fallback")]
    public async Task UnwritableSinkFallsBackWithoutSecondaryFailure()
    {
        string unwritable = Path.Combine(Path.GetTempPath(), $"vicione-{Guid.NewGuid():N}", "nested", "out.json");

        int code = await Program.Main(["publish-load", "--messages", "0", "--output", unwritable]);

        Assert.Equal(1, code);
        Assert.False(File.Exists(unwritable));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-DELIVERY", "fallback-preserves-result")]
    public async Task FallbackOutputCarriesUndeliveredResult()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"vicione-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var output = new StringWriter();
        var error = new StringWriter();

        try
        {
            bool delivered = await Program.Report(new { status = "failed", error = "a reason" }, directory,
                CancellationToken.None, output, error);

            Assert.False(delivered);
            using JsonDocument written = JsonDocument.Parse(output.ToString());
            Assert.Equal("failed", written.RootElement.GetProperty("status").GetString());
            Assert.Equal("a reason", written.RootElement.GetProperty("error").GetString());
            Assert.Contains("the result could not be written to", error.ToString(), StringComparison.Ordinal);
            Assert.Contains(directory, error.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-DELIVERY", "standard-output-without-file")]
    public async Task NoRequestedFileWritesStandardOutput()
    {
        var output = new StringWriter();

        bool delivered = await Program.Report(new { status = "ok" }, null, CancellationToken.None, output);

        Assert.True(delivered);
        using JsonDocument written = JsonDocument.Parse(output.ToString());
        Assert.Equal("ok", written.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-DELIVERY", "file-delivered-once")]
    public async Task WrittenFileReportsDeliveredOnce()
    {
        using var sink = new TemporaryFile();
        var output = new StringWriter();

        bool delivered = await Program.Report(new { status = "ok" }, sink.Path, CancellationToken.None, output);

        Assert.True(delivered);
        Assert.Empty(output.ToString());
        Assert.True(File.Exists(sink.Path));
        using JsonDocument written = JsonDocument.Parse(
            await File.ReadAllTextAsync(sink.Path, TestContext.Current.CancellationToken));
        Assert.Equal("ok", written.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-DELIVERY", "undeliverable-result-fails-run")]
    public async Task UndeliverableSuccessfulResultReturnsFailure()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"vicione-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var output = new StringWriter();

        try
        {
            int code = await Program.Deliver(new { status = "ok" }, directory, CancellationToken.None,
                output, new StringWriter());

            Assert.NotEqual(0, code);
            Assert.Contains("\"status\"", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-DELIVERY", "delivered-result-succeeds-run")]
    public async Task DeliveredSuccessfulResultReturnsSuccess()
    {
        using var sink = new TemporaryFile();

        int code = await Program.Deliver(new { status = "ok" }, sink.Path, CancellationToken.None,
            new StringWriter(), new StringWriter());

        Assert.Equal(0, code);
        Assert.True(File.Exists(sink.Path));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-CANCELLATION", "handler-removed")]
    public void CancellationHandlerIsRemoved()
    {
        var subscribed = 0;
        var removed = 0;
        using var cancellation = new CancellationTokenSource();

        IDisposable interrupt = Program.HandleCancellation(cancellation, _ => subscribed++, _ => removed++);

        Assert.Equal(1, subscribed);
        Assert.Equal(0, removed);
        interrupt.Dispose();
        Assert.Equal(1, removed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-CANCELLATION", "handler-removed-once")]
    public void CancellationHandlerIsRemovedOnce()
    {
        var removed = 0;
        using var cancellation = new CancellationTokenSource();

        IDisposable interrupt = Program.HandleCancellation(cancellation, _ => { }, _ => removed++);
        interrupt.Dispose();
        interrupt.Dispose();

        Assert.Equal(1, removed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-CANCELLATION", "interrupt-cancels-without-exit")]
    public void InterruptCancelsRunWithoutTerminatingProcess()
    {
        ConsoleCancelEventHandler? captured = null;
        using var cancellation = new CancellationTokenSource();
        using IDisposable interrupt = Program.HandleCancellation(cancellation, handler => captured = handler, _ => { });
        ConsoleCancelEventArgs eventArgs = ConsoleCancelEventArgsFactory();

        captured!(null, eventArgs);

        Assert.True(cancellation.IsCancellationRequested);
        Assert.True(eventArgs.Cancel);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-DELIVERY", "unknown-scenario-without-sink")]
    public async Task UnknownScenarioReturnsFailureWithoutSink()
    {
        Assert.Equal(1, await Program.Main(["not-a-scenario"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTICS-DELIVERY", "unparseable-options-no-file")]
    public async Task UnparseableOptionsCreateNoFile()
    {
        using var sink = new TemporaryFile();

        int code = await Program.Main(["publish-load", "--output", sink.Path, "--not-an-option", "x"]);

        Assert.Equal(1, code);
        Assert.False(File.Exists(sink.Path));
    }

    private static ConsoleCancelEventArgs ConsoleCancelEventArgsFactory()
    {
        return (ConsoleCancelEventArgs)RuntimeHelpers.GetUninitializedObject(typeof(ConsoleCancelEventArgs));
    }

    private sealed class TemporaryFile : IDisposable
    {
        public TemporaryFile()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                $"vicione-diagnostics-{Guid.NewGuid():N}.json");
        }

        public string Path { get; }

        public void Dispose()
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }
    }
}
