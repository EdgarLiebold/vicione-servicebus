using System.Reflection;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Azure.Storage.Blobs;
using ViciOne.ServiceBus.EventHubs.Middleware;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EventHubs;

if (args.Length != 1) throw new ArgumentException("One case required");
if (args[0] == "callback-fault")
{
    var control = await Closing(false);
    Require(control.Sequence.SequenceEqual(new[] { "application:close", "internal:close" }), "Positive closing control failed");
    var fault = await Closing(true);
    Require(fault.OriginalFaultPreserved, "Application fault identity was lost");
    bool cleaned = fault.Sequence.SequenceEqual(new[] { "application:close", "internal:close" });
    Console.WriteLine(JsonSerializer.Serialize(new { positive_control = control, fault_case = fault, contract = cleaned ? "PASS" : "FAIL" }));
    return cleaned ? 0 : 2;
}
if (args[0] == "invalid-config")
{
    var control = await Build("valid");
    Require(control.BuildCompleted && control.CompositionValidationCompleted, "Valid configuration control failed: " + control.Error);
    List<BuildResult> variants = [];
    foreach (string name in new[] { "count-zero", "limit-zero", "interval-negative", "delivery-zero", "prefetch-zero" })
        variants.Add(await Build(name));
    bool rejected = variants.All(x => !x.BuildCompleted || !x.CompositionValidationCompleted);
    Console.WriteLine(JsonSerializer.Serialize(new { positive_control = control, variants, bus_started = false, contract = rejected ? "PASS" : "FAIL" }));
    return rejected ? 0 : 2;
}
if (args[0] == "sas-probe")
{
    var controlClient = new BlobContainerClient(new Uri("https://owned-review.blob.core.windows.net/owned-container"));
    var controlFilter = new EventHubBlobContainerFactoryFilter(controlClient);
    string control = JsonSerializer.Serialize(controlFilter.GetProbeResult().Results);
    using var controlDocument = JsonDocument.Parse(control);
    var controlScope = controlDocument.RootElement.GetProperty("filters");
    var controlUri = new Uri(controlScope.GetProperty("Uri").GetString()!);
    Require(controlScope.GetProperty("filterType").GetString() == "configureTopology", "Positive probe scope missing");
    Require(controlUri.Scheme == "https" && controlUri.Host == "owned-review.blob.core.windows.net"
        && controlUri.Port == 443 && controlUri.AbsolutePath == "/owned-container"
        && controlUri.Query.Length == 0 && controlUri.Fragment.Length == 0 && controlUri.UserInfo.Length == 0,
        "Positive URI control missing");
    Require(controlScope.GetProperty("Name").GetString() == "owned-container", "Positive container name missing");
    const string canary = "owned-review-canary";
    var sasClient = new BlobContainerClient(new Uri("https://owned-review.blob.core.windows.net/owned-container?sv=2024-11-04&sr=c&sp=rw&se=2030-01-01T00%3A00%3A00Z&sig=" + canary));
    var filter = new EventHubBlobContainerFactoryFilter(sasClient);
    string snapshot = JsonSerializer.Serialize(filter.GetProbeResult().Results);
    using var snapshotDocument = JsonDocument.Parse(snapshot);
    var snapshotScope = snapshotDocument.RootElement.GetProperty("filters");
    var snapshotUri = new Uri(snapshotScope.GetProperty("Uri").GetString()!);
    Require(snapshotUri.Scheme == controlUri.Scheme && snapshotUri.Host == controlUri.Host
        && snapshotUri.Port == controlUri.Port && snapshotUri.AbsolutePath == controlUri.AbsolutePath,
        "SAS probe lost container identity");
    Require(snapshotScope.GetProperty("Name").GetString() == "owned-container", "SAS probe lost container name");
    Require(sasClient.Uri.Query.Contains(canary, StringComparison.Ordinal), "SDK SAS URI was changed");
    bool leaked = snapshot.Contains(canary, StringComparison.Ordinal)
        || snapshotUri.Query.Length != 0 || snapshotUri.Fragment.Length != 0 || snapshotUri.UserInfo.Length != 0;
    Console.WriteLine(JsonSerializer.Serialize(new { positive_control = control.Contains("owned-container", StringComparison.Ordinal), sdk_uri_has_signature = sasClient.Uri.Query.Contains(canary, StringComparison.Ordinal), probe_results_has_signature = leaked, contract = leaked ? "FAIL" : "PASS" }));
    return leaked ? 2 : 0;
}
throw new ArgumentException("Unknown case");

static async Task<ClosingResult> Closing(bool throwApplication)
{
    List<string> calls = [];
    IHostConfiguration host = DispatchProxy.Create<IHostConfiguration, UnexpectedProxy>();
    var client = new ControlledProcessor();
    var original = new InvalidOperationException("owned-application-close-fault");
    var context = new EventHubProcessorContext(host, client, null, _ =>
    {
        calls.Add("application:close");
        return throwApplication ? Task.FromException(original) : Task.CompletedTask;
    }, CancellationToken.None);
    context.GetClient(new RecordingBuilder(calls));
    bool preserved = false;
    try { await client.ClosePartition(); }
    catch (InvalidOperationException ex) when (ReferenceEquals(ex, original)) { preserved = true; }
    finally { context.ReleaseClient(); }
    return new ClosingResult(calls.ToArray(), preserved);
}
static async Task<BuildResult> Build(string name)
{
    var services = new ServiceCollection();
    services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
    services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    services.AddViciOneServiceBus(bus =>
    {
        bus.Limits(MessageLimits.Conservative);
        bus.UsingInMemory();
        bus.AddRider(rider => rider.UsingEventHub((_, hubs) =>
        {
            hubs.Host("Endpoint=sb://review.servicebus.windows.net/;SharedAccessKeyName=review;SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
            hubs.Storage("UseDevelopmentStorage=true");
            hubs.ReceiveEndpoint("owned-review", "owned-group", endpoint =>
            {
                endpoint.ContainerName = "owned-review";
                endpoint.CheckpointMessageCount = 1;
                endpoint.CheckpointMessageLimit = 1;
                endpoint.CheckpointInterval = TimeSpan.FromMinutes(1);
                endpoint.ConcurrentDeliveryLimit = 1;
                endpoint.PrefetchCount = 1;
                switch (name)
                {
                    case "count-zero": endpoint.CheckpointMessageCount = 0; break;
                    case "limit-zero": endpoint.CheckpointMessageLimit = 0; break;
                    case "interval-negative": endpoint.CheckpointInterval = TimeSpan.FromMilliseconds(-2); break;
                    case "delivery-zero": endpoint.ConcurrentDeliveryLimit = 0; break;
                    case "prefetch-zero": endpoint.PrefetchCount = 0; break;
                }
            });
        }));
    });
    bool built = false;
    await using var provider = services.BuildServiceProvider(true);
    try
    {
        var bus = provider.GetRequiredService<IBusControl>();
        Require(bus != null, "Resolved bus null");
        built = true;
        var validators = provider.GetServices<IHostedService>().Where(x => x.GetType().Name.StartsWith("BusCompositionStartupValidator", StringComparison.Ordinal)).ToArray();
        Require(validators.Length == 1, "Expected exactly one bus composition validator");
        await validators[0].StartAsync(CancellationToken.None);
        return new BuildResult(name, true, true, null);
    }
    catch (ConfigurationException ex) { return new BuildResult(name, built, false, ex.Message); }
}
static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
public sealed record ClosingResult(string[] Sequence, bool OriginalFaultPreserved);
public sealed record BuildResult(string Case, bool BuildCompleted, bool CompositionValidationCompleted, string? Error);
public class UnexpectedProxy : DispatchProxy
{
    protected override object? Invoke(MethodInfo? method, object?[]? args) => throw new NotSupportedException(method?.Name);
}
public sealed class RecordingBuilder(List<string> calls) : ProcessorClientBuilderContext
{
    public Task OnPartitionInitializingAsync(PartitionInitializingEventArgs args, CancellationToken token = default) => Task.CompletedTask;
    public Task OnPartitionClosingAsync(PartitionClosingEventArgs args, CancellationToken token = default) { calls.Add("internal:close"); return Task.CompletedTask; }
}
public sealed class ControlledProcessor : EventProcessorClient
{
    public Task ClosePartition() => OnPartitionProcessingStoppedAsync(new ControlledPartition(), ProcessingStoppedReason.Shutdown, CancellationToken.None);
}
public sealed class ControlledPartition : EventProcessorPartition
{
    public ControlledPartition() => PartitionId = "owned-partition";
}
