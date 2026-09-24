using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Architecture;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class OutboundNetworkBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-NO-OUTBOUND-VENDOR-CALL", "default-bus-lifecycle-and-publish")]
    public async Task DefaultInMemoryBusLifecycle_IssuesNoOutgoingHttpRequestAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var requests = new ConcurrentQueue<string>();
        using IDisposable subscription = DiagnosticListener.AllListeners.Subscribe(new ListenerObserver(requests));
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.AddConsumer<QuietConsumer>();
                configuration.UsingInMemory((context, transport) => transport.ConfigureEndpoints(context));
            })
            .BuildServiceProvider(validateScopes: true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await bus.PublishAsync(new QuietMessage(), cancellationToken).WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Empty(requests);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NO-OUTBOUND-VENDOR-CALL", "diagnostic-listener-positive-control")]
    public void HttpDiagnosticListener_ObservesASyntheticRequestStartWithoutOpeningASocket()
    {
        var requests = new ConcurrentQueue<string>();
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1/vicione-detector-control");
        using IDisposable subscription = DiagnosticListener.AllListeners.Subscribe(new ListenerObserver(requests, request));
        using var listener = new DiagnosticListener("HttpHandlerDiagnosticListener");

        listener.Write("System.Net.Http.HttpRequestOut.Start", new { Request = request });

        Assert.Equal([request.RequestUri!.ToString()], requests);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NO-OUTBOUND-VENDOR-CALL", "assembly-address-scan-with-positive-control")]
    public void ProductAssemblyBytes_ContainNoVendorAddressAndDoContainAddressLikeStrings()
    {
        string[] strings = RemovalBoundaryTests.ProductAssemblies()
            .SelectMany(RemovalBoundaryTests.AssemblyStrings)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        string[] addressLike = strings.Where(text => text.Contains("://", StringComparison.Ordinal)).ToArray();

        string[] probe =
        [
            "loopback://localhost/",
            "https://github.com/EdgarLiebold/vicione-servicebus/usage/containers/multibus.html",
            "https://vendor.invalid/usage",
            "https://vendor.invalid/telemetry",
            "https://vendor.invalid/license",
        ];
        Assert.Equal(
            ["https://vendor.invalid/usage", "https://vendor.invalid/telemetry", "https://vendor.invalid/license"],
            FindForbiddenVendorAddresses(probe));

        string[] offenders = FindForbiddenVendorAddresses(addressLike);

        Assert.Contains("loopback://localhost/", addressLike);
        Assert.Empty(offenders);
    }

    private static string[] FindForbiddenVendorAddresses(IEnumerable<string> strings) => strings
        .SelectMany(text => Regex.Matches(text, @"https?://[^\s<>]+")
            .Select(match => match.Value))
        .Where(address => !string.Equals(address,
                "https://github.com/EdgarLiebold/vicione-servicebus/usage/containers/multibus.html",
                StringComparison.OrdinalIgnoreCase)
            && (address.Contains("usage", StringComparison.OrdinalIgnoreCase)
                || address.Contains("telemetry", StringComparison.OrdinalIgnoreCase)
                || address.Contains("license", StringComparison.OrdinalIgnoreCase)))
        .ToArray();

    [Fact]
    [RequirementCoverage("REQ-VSB-NO-OUTBOUND-VENDOR-CALL", "two-distinct-product-assemblies")]
    public void OutboundAddressScan_ReadsTwoDistinctProductAssemblies()
    {
        Assembly[] assemblies = RemovalBoundaryTests.ProductAssemblies().ToArray();
        string[] names = assemblies.Select(assembly => Assert.IsType<string>(assembly.GetName().Name)).ToArray();

        Assert.Equal(2, assemblies.Length);
        Assert.Equal(2, names.Distinct(StringComparer.Ordinal).Count());
        Assert.All(assemblies, assembly => Assert.NotEmpty(RemovalBoundaryTests.AssemblyStrings(assembly)));
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    public sealed record QuietMessage;

    public sealed class QuietConsumer : IConsumer<QuietMessage>
    {
        public Task ConsumeAsync(ConsumeContext<QuietMessage> context) => Task.CompletedTask;
    }

    private sealed class ListenerObserver(ConcurrentQueue<string> requests, HttpRequestMessage? expectedRequest = null) :
        IObserver<DiagnosticListener>
    {
        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "HttpHandlerDiagnosticListener")
                listener.Subscribe(new EventObserver(requests, expectedRequest));
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }
    }

    private sealed class EventObserver(ConcurrentQueue<string> requests, HttpRequestMessage? expectedRequest) :
        IObserver<KeyValuePair<string, object?>>
    {
        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (!value.Key.EndsWith(".Start", StringComparison.Ordinal))
                return;

            var request = value.Value?.GetType().GetProperty("Request")?.GetValue(value.Value) as HttpRequestMessage;
            if (expectedRequest is not null && !ReferenceEquals(request, expectedRequest))
                return;

            requests.Enqueue(request?.RequestUri?.ToString() ?? value.Key);
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }
    }
}
