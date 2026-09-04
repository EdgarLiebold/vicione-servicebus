using System.Text.Json;

namespace ViciOne.ServiceBus.Tests.Infrastructure.Brokers;

/// <summary>
/// Exchanges interruption requests with the canonical fixture runner without owning Docker.
/// </summary>
public sealed class BrokerOutageControlClient
{
    public const string ControlVariable = "VICIONE_SERVICEBUS_FIXTURE_CONTROL";
    public const int SchemaVersion = 1;

    private static readonly TimeSpan DefaultBudget = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan ResultProbeInterval = TimeSpan.FromMilliseconds(25);

    private readonly string _controlDirectory;
    private readonly Action<string>? _onRequestPublished;
    private readonly TimeProvider _timeProvider;

    public BrokerOutageControlClient(string controlDirectory)
        : this(controlDirectory, TimeProvider.System)
    {
    }

    internal BrokerOutageControlClient(
        string controlDirectory,
        TimeProvider timeProvider,
        Action<string>? onRequestPublished = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controlDirectory);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (!Directory.Exists(controlDirectory))
            throw new DirectoryNotFoundException($"The fixture control directory '{controlDirectory}' does not exist.");

        _controlDirectory = Path.GetFullPath(controlDirectory);
        _timeProvider = timeProvider;
        _onRequestPublished = onRequestPublished;
    }

    public static BrokerOutageControlClient FromEnvironment()
    {
        string? controlDirectory = Environment.GetEnvironmentVariable(ControlVariable);
        if (string.IsNullOrWhiteSpace(controlDirectory))
        {
            throw new InvalidOperationException(
                $"{ControlVariable} is missing. Start the fixture with " +
                "tools/ci/run_broker_category.py --allow-broker-outage activemq.");
        }

        return new BrokerOutageControlClient(controlDirectory);
    }

    /// <summary>Returns only after the runner has observed the broker stopped or absent.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public Task InterruptAsync(CancellationToken cancellationToken = default) =>
        RequestAsync("interrupt", DefaultBudget, cancellationToken);

    /// <summary>
    /// Returns only after the runner has observed the broker healthy. Cleanup deliberately has no
    /// caller cancellation token: a failed test must still restore the run-owned broker.
    /// </summary>
    public Task RestoreAsync() => RequestAsync("restore", DefaultBudget, CancellationToken.None);

    internal async Task RequestAsync(string action, TimeSpan budget, CancellationToken cancellationToken = default)
    {
        if (action is not ("interrupt" or "restore"))
            throw new ArgumentOutOfRangeException(nameof(action), action, "Expected interrupt or restore.");
        if (budget <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(budget), budget, "The response budget must be positive.");

        string requestId = $"{action}-{Guid.NewGuid():N}";
        string requestPath = Path.Combine(_controlDirectory, $"{requestId}.request");
        string resultPath = Path.Combine(_controlDirectory, $"{requestId}.result");

        Task timeout = Task.Delay(budget, _timeProvider, cancellationToken);

        string partialPath = requestPath + ".partial";
        await File.WriteAllTextAsync(
            partialPath,
            JsonSerializer.Serialize(new { schemaVersion = SchemaVersion, requestId, action }),
            cancellationToken).ConfigureAwait(false);
        File.Move(partialPath, requestPath);
        _onRequestPublished?.Invoke(requestPath);

        // FileSystemWatcher notifications are lossy: operating systems may coalesce or drop them.
        // The protocol therefore derives completion from the atomically renamed result file itself
        // and bounds its probes through the injected TimeProvider.
        while (!File.Exists(resultPath))
        {
            Task probe = Task.Delay(ResultProbeInterval, _timeProvider, cancellationToken);
            Task completed = await Task.WhenAny(probe, timeout).ConfigureAwait(false);
            if (completed == timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException(
                    $"The fixture runner did not answer the request to {action} the broker within " +
                    $"{budget.TotalSeconds:0.###} seconds.");
            }

            await probe.ConfigureAwait(false);
        }

        string json = await File.ReadAllTextAsync(resultPath, cancellationToken).ConfigureAwait(false);
        BrokerOutageAnswer answer = ReadAnswer(json, requestId, action);
        if (!answer.IsSuccess)
        {
            throw new BrokerOutageControlException(
                $"The fixture runner refused to {action} the broker: {answer.Error ?? "no reason given"}");
        }
    }

    internal static BrokerOutageAnswer ReadAnswer(string json, string requestId, string action)
    {
        JsonElement root;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            root = document.RootElement.Clone();
        }
        catch (JsonException exception)
        {
            return BrokerOutageAnswer.Failure($"The fixture runner answer is not readable JSON: {exception.Message}");
        }

        if (root.ValueKind != JsonValueKind.Object)
            return BrokerOutageAnswer.Failure("The fixture runner answer is not a JSON object.");

        int version = root.TryGetProperty("schemaVersion", out JsonElement schema) && schema.TryGetInt32(out int parsed)
            ? parsed
            : 0;
        if (version != SchemaVersion)
        {
            return BrokerOutageAnswer.Failure(
                $"The fixture runner answered with schema version {version}; this client speaks {SchemaVersion}.");
        }

        string? echoedId = root.TryGetProperty("requestId", out JsonElement identity) ? identity.GetString() : null;
        if (!string.Equals(echoedId, requestId, StringComparison.Ordinal))
            return BrokerOutageAnswer.Failure($"The answer carries request id '{echoedId}', not '{requestId}'.");

        string? echoedAction = root.TryGetProperty("action", out JsonElement performed) ? performed.GetString() : null;
        if (!string.Equals(echoedAction, action, StringComparison.Ordinal))
            return BrokerOutageAnswer.Failure($"The answer is about '{echoedAction}', not '{action}'.");

        string? status = root.TryGetProperty("status", out JsonElement reported) ? reported.GetString() : null;
        string? error = root.TryGetProperty("error", out JsonElement reason) ? reason.GetString() : null;
        if (!string.Equals(status, "ok", StringComparison.Ordinal))
            return BrokerOutageAnswer.Failure(error);

        string? observed = root.TryGetProperty("observed", out JsonElement state) ? state.GetString() : null;
        string[] accepted = action == "interrupt"
            ? ["exited", "stopped", "absent"]
            : ["healthy"];

        if (string.IsNullOrEmpty(observed))
            return BrokerOutageAnswer.Failure($"The runner reported success for '{action}' without an observed state.");
        if (!accepted.Contains(observed, StringComparer.Ordinal))
        {
            return BrokerOutageAnswer.Failure(
                $"The runner reported success for '{action}' after observing '{observed}'; expected " +
                $"one of: {string.Join(", ", accepted)}.");
        }

        return BrokerOutageAnswer.Success;
    }
}

public sealed class BrokerOutageControlException(string message) : InvalidOperationException(message);

internal readonly record struct BrokerOutageAnswer(bool IsSuccess, string? Error)
{
    public static BrokerOutageAnswer Success { get; } = new(true, null);

    public static BrokerOutageAnswer Failure(string? error) => new(false, error);
}
