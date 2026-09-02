using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Brokers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests.Brokers;

public sealed class BrokerOutageControlClientTests
{
    [Theory]
    [InlineData("exited", true)]
    [InlineData("stopped", true)]
    [InlineData("absent", true)]
    [InlineData("running", false)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-OUTAGE", "interrupt-observed-state-contract")]
    public void Interrupt_RequiresObservedStoppedOrAbsentState(string observed, bool expectedSuccess)
    {
        BrokerOutageAnswer answer = BrokerOutageControlClient.ReadAnswer(
            Answer("interrupt-1", "interrupt", "ok", observed: observed),
            "interrupt-1",
            "interrupt");

        Assert.Equal(expectedSuccess, answer.IsSuccess);
        if (!expectedSuccess)
            Assert.Contains("running", answer.Error, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-OUTAGE", "restore-failure-is-propagated")]
    public async Task RestoreFailure_IsReported()
    {
        using var control = new ControlDirectory();
        BrokerOutageControlClient client = control.CreateClient();

        Task operation = client.RequestAsync(
            "restore",
            TimeSpan.FromMinutes(1),
            TestContext.Current.CancellationToken);
        control.RespondTo(
            await control.RequestPublished,
            status: "failed",
            error: "the broker was still starting after restore");

        BrokerOutageControlException exception = await Assert.ThrowsAsync<BrokerOutageControlException>(() => operation);
        Assert.Contains("still starting after restore", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-OUTAGE", "unanswered-request-times-out")]
    public async Task UnansweredRequest_TimesOut()
    {
        using var control = new ControlDirectory();
        var time = new FakeTimeProvider();
        BrokerOutageControlClient client = control.CreateClient(time);

        Task operation = client.RequestAsync(
            "interrupt",
            TimeSpan.FromMinutes(1),
            TestContext.Current.CancellationToken);
        _ = await control.RequestPublished;
        time.Advance(TimeSpan.FromMinutes(1));

        TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(() => operation);
        Assert.Contains("did not answer", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-OUTAGE", "rejected-interrupt-is-propagated")]
    public async Task RejectedInterrupt_IsReported()
    {
        using var control = new ControlDirectory();
        BrokerOutageControlClient client = control.CreateClient();

        Task operation = client.RequestAsync(
            "interrupt",
            TimeSpan.FromMinutes(1),
            TestContext.Current.CancellationToken);
        control.RespondTo(
            await control.RequestPublished,
            status: "failed",
            error: "docker compose could not stop activemq");

        BrokerOutageControlException exception = await Assert.ThrowsAsync<BrokerOutageControlException>(() => operation);
        Assert.Contains("could not stop activemq", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-OUTAGE", "response-matches-request-and-action")]
    public void Response_MatchesTheExactRequest()
    {
        BrokerOutageAnswer exact = BrokerOutageControlClient.ReadAnswer(
            Answer("interrupt-1", "interrupt", "ok"),
            "interrupt-1",
            "interrupt");
        BrokerOutageAnswer anotherRequest = BrokerOutageControlClient.ReadAnswer(
            Answer("interrupt-2", "interrupt", "ok"),
            "interrupt-1",
            "interrupt");

        Assert.True(exact.IsSuccess);
        Assert.False(anotherRequest.IsSuccess);
        Assert.Contains("interrupt-2", anotherRequest.Error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("schema")]
    [InlineData("action")]
    [InlineData("missing-observed")]
    [InlineData("opposite-state")]
    [InlineData("invalid-json")]
    [InlineData("non-object")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-OUTAGE", "malformed-or-mismatched-answer-rejected")]
    public void Protocol_RejectsUnconfirmedOrMismatchedResponses(string fault)
    {
        const string requestId = "restore-1";
        string response = fault switch
        {
            "schema" => JsonSerializer.Serialize(new
            {
                schemaVersion = BrokerOutageControlClient.SchemaVersion + 1,
                requestId,
                action = "restore",
                status = "ok",
                observed = "healthy",
            }),
            "action" => Answer(requestId, "interrupt", "ok"),
            "missing-observed" => JsonSerializer.Serialize(new
            {
                schemaVersion = BrokerOutageControlClient.SchemaVersion,
                requestId,
                action = "restore",
                status = "ok",
            }),
            "opposite-state" => Answer(requestId, "restore", "ok", observed: "exited"),
            "invalid-json" => "{ this is not json",
            "non-object" => "[]",
            _ => throw new InvalidOperationException(fault),
        };

        BrokerOutageAnswer answer = BrokerOutageControlClient.ReadAnswer(response, requestId, "restore");

        Assert.False(answer.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(answer.Error));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-OUTAGE", "operation-completes-after-observed-effect")]
    public async Task Interrupt_ReturnsOnlyAfterTheObservedEffect()
    {
        using var control = new ControlDirectory();
        BrokerOutageControlClient client = control.CreateClient();

        Task operation = client.RequestAsync(
            "interrupt",
            TimeSpan.FromMinutes(1),
            TestContext.Current.CancellationToken);
        string request = await control.RequestPublished;

        Assert.False(operation.IsCompleted);

        control.RespondTo(request, status: "ok", observed: "stopped");
        await operation;

        Assert.True(operation.IsCompletedSuccessfully);
    }

    private static string Answer(
        string requestId,
        string action,
        string status,
        string? error = null,
        string? observed = null) =>
        JsonSerializer.Serialize(new
        {
            schemaVersion = BrokerOutageControlClient.SchemaVersion,
            requestId,
            action,
            status,
            error,
            observed = observed ?? (action == "interrupt" ? "exited" : "healthy"),
        });

    private sealed class ControlDirectory : IDisposable
    {
        private readonly TaskCompletionSource<string> _requestPublished =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ControlDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"vicione-activemq-control-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public Task<string> RequestPublished => _requestPublished.Task;

        public BrokerOutageControlClient CreateClient(TimeProvider? timeProvider = null) =>
            new(
                Path,
                timeProvider ?? TimeProvider.System,
                requestPath => _requestPublished.TrySetResult(requestPath));

        public void RespondTo(
            string requestPath,
            string status,
            string? error = null,
            string? observed = null)
        {
            using JsonDocument request = JsonDocument.Parse(File.ReadAllText(requestPath));
            string requestId = request.RootElement.GetProperty("requestId").GetString()
                ?? throw new InvalidDataException("Request id is missing.");
            string action = request.RootElement.GetProperty("action").GetString()
                ?? throw new InvalidDataException("Action is missing.");
            string result = System.IO.Path.Combine(Path, $"{requestId}.result");
            string partial = result + ".partial";

            File.WriteAllText(partial, Answer(requestId, action, status, error, observed));
            File.Move(partial, result);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }
    }
}
