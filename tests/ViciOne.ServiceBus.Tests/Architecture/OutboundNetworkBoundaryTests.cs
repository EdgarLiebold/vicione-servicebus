using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Architecture;

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
        using IDisposable subscription = DiagnosticListener.AllListeners.Subscribe(new ListenerObserver(requests));
        using var listener = new DiagnosticListener("HttpHandlerDiagnosticListener");
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1/vicione-detector-control");

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
        string[] offenders = addressLike
            .Where(text => !text.StartsWith("loopback://", StringComparison.OrdinalIgnoreCase)
                && !text.StartsWith("urn:", StringComparison.OrdinalIgnoreCase)
                && (text.Contains("usage", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("telemetry", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("license", StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.NotEmpty(addressLike);
        Assert.Empty(offenders);
    }

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

    private sealed class ListenerObserver(ConcurrentQueue<string> requests) : IObserver<DiagnosticListener>
    {
        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "HttpHandlerDiagnosticListener")
                listener.Subscribe(new EventObserver(requests));
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }
    }

    private sealed class EventObserver(ConcurrentQueue<string> requests) : IObserver<KeyValuePair<string, object?>>
    {
        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (!value.Key.EndsWith(".Start", StringComparison.Ordinal))
                return;

            var request = value.Value?.GetType().GetProperty("Request")?.GetValue(value.Value) as HttpRequestMessage;
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
