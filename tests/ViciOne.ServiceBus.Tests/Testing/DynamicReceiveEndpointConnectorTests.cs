using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class DynamicReceiveEndpointConnectorTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TESTING-DYNAMIC-ENDPOINT", "duplicate-endpoint-name-is-rejected")]
    public async Task DynamicConnector_RejectsASecondEndpointWithTheSameNameAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string endpointName = $"duplicate-{NewId.NextGuid():N}";
        using var harness = new InMemoryTestHarness($"duplicate-host-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        IHostReceiveEndpointHandle? first = null;

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            first = harness.Bus.ConnectReceiveEndpoint(endpointName, _ => { });
            ReceiveEndpointReady ready = await first.Ready.WaitAsync(timeout, cancellationToken);

            ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(async () =>
            {
                IHostReceiveEndpointHandle duplicate = harness.Bus.ConnectReceiveEndpoint(endpointName, _ => { });
                await duplicate.Ready.WaitAsync(timeout, cancellationToken);
            });

            Assert.Equal(new Uri(harness.BaseAddress, endpointName), ready.InputAddress);
            Assert.Contains(endpointName, exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            if (first is not null)
                await first.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);

            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TESTING-DYNAMIC-ENDPOINT", "connector-overloads-and-registration-context")]
    public async Task DynamicConnector_UsesTheRegistrationContextForNamedAndDefinedEndpointsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var marker = new RegistrationMarker();
        var configuredEndpoints = new ConcurrentQueue<string>();
        var providerConfiguredEndpoints = new ConcurrentQueue<string>();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(marker)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConfigureEndpointsCallback((name, _) => configuredEndpoints.Enqueue(
                    name ?? throw new Xunit.Sdk.XunitException("Expected a configured endpoint name.")));
                configuration.AddConfigureEndpointsCallback((context, name, _) =>
                {
                    Assert.Same(marker, context.GetRequiredService<RegistrationMarker>());
                    providerConfiguredEndpoints.Enqueue(
                        name ?? throw new Xunit.Sdk.XunitException("Expected a provider-configured endpoint name."));
                });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        IReceiveEndpointConnector connector = provider.GetRequiredService<IReceiveEndpointConnector>();
        TaskCompletionSource<ConsumeContext<NamedEndpointMessage>> namedConsumed = harness.GetTask<ConsumeContext<NamedEndpointMessage>>();
        TaskCompletionSource<ConsumeContext<DefinedEndpointMessage>> definedConsumed = harness.GetTask<ConsumeContext<DefinedEndpointMessage>>();
        IBusRegistrationContext? namedRegistration = null;
        IBusRegistrationContext? definedRegistration = null;
        IHostReceiveEndpointHandle? namedEndpoint = null;
        IHostReceiveEndpointHandle? definedEndpoint = null;

        try
        {
            namedEndpoint = connector.ConnectReceiveEndpoint(
                $"named-{NewId.NextGuid():N}",
                (context, configurator) =>
                {
                    namedRegistration = context;
                    Assert.Same(marker, context.GetRequiredService<RegistrationMarker>());
                    configurator.Handler<NamedEndpointMessage>(async consumeContext =>
                    {
                        namedConsumed.TrySetResult(consumeContext);
                        await consumeContext.Advanced().PublishAsync(
                            new NamedEndpointEvent(consumeContext.Message.CorrelationId),
                            consumeContext.CancellationToken);
                    });
                });
            definedEndpoint = connector.ConnectReceiveEndpoint(
                new TemporaryEndpointDefinition(),
                KebabCaseEndpointNameFormatter.Instance,
                (context, configurator) =>
                {
                    definedRegistration = context;
                    Assert.Same(marker, context.GetRequiredService<RegistrationMarker>());
                    configurator.Handler<DefinedEndpointMessage>(async consumeContext =>
                    {
                        definedConsumed.TrySetResult(consumeContext);
                        await consumeContext.Advanced().PublishAsync(
                            new DefinedEndpointEvent(consumeContext.Message.CorrelationId),
                            consumeContext.CancellationToken);
                    });
                });

            Assert.NotNull(namedRegistration);
            Assert.NotNull(definedRegistration);

            ReceiveEndpointReady namedReady = await namedEndpoint.Ready.WaitAsync(timeout, cancellationToken);
            ReceiveEndpointReady definedReady = await definedEndpoint.Ready.WaitAsync(timeout, cancellationToken);
            var namedMessage = new NamedEndpointMessage(NewId.NextGuid());
            var definedMessage = new DefinedEndpointMessage(NewId.NextGuid());
            ISendEndpoint namedSendEndpoint = await harness.Bus.GetSendEndpointAsync(namedReady.InputAddress, TestContext.Current.CancellationToken);
            ISendEndpoint definedSendEndpoint = await harness.Bus.GetSendEndpointAsync(definedReady.InputAddress, TestContext.Current.CancellationToken);

            await namedSendEndpoint.SendAsync(namedMessage, cancellationToken);
            await definedSendEndpoint.SendAsync(definedMessage, cancellationToken);

            ConsumeContext<NamedEndpointMessage> namedContext = await namedConsumed.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<DefinedEndpointMessage> definedContext = await definedConsumed.Task.WaitAsync(timeout, cancellationToken);
            IPublishedMessage<NamedEndpointEvent> namedEvent = await harness.Published
                .SelectAsync<NamedEndpointEvent>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            IPublishedMessage<DefinedEndpointEvent> definedEvent = await harness.Published
                .SelectAsync<DefinedEndpointEvent>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            string namedEndpointName = namedReady.InputAddress.AbsolutePath.Trim('/');
            string definedEndpointName = definedReady.InputAddress.AbsolutePath.Trim('/');
            Assert.Equal(1, configuredEndpoints.Count(name => name == namedEndpointName));
            Assert.Equal(1, configuredEndpoints.Count(name => name == definedEndpointName));
            Assert.Equal(1, providerConfiguredEndpoints.Count(name => name == namedEndpointName));
            Assert.Equal(1, providerConfiguredEndpoints.Count(name => name == definedEndpointName));
            Assert.Equal(namedReady.InputAddress, namedContext.Advanced().ReceiveContext.InputAddress);
            Assert.Equal(definedReady.InputAddress, definedContext.Advanced().ReceiveContext.InputAddress);
            Assert.NotEqual(namedContext.Advanced().ReceiveContext.InputAddress, definedContext.Advanced().ReceiveContext.InputAddress);
            Assert.Equal(namedMessage.CorrelationId, namedEvent.Context.Message.CorrelationId);
            Assert.Equal(definedMessage.CorrelationId, definedEvent.Context.Message.CorrelationId);
            Assert.Equal(namedContext.CorrelationId, namedEvent.Context.InitiatorId);
            Assert.Equal(definedContext.CorrelationId, definedEvent.Context.InitiatorId);
        }
        finally
        {
            if (definedEndpoint is not null)
                await definedEndpoint.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            if (namedEndpoint is not null)
                await namedEndpoint.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);

            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class RegistrationMarker;

    private sealed record NamedEndpointMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record DefinedEndpointMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record NamedEndpointEvent(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record DefinedEndpointEvent(Guid CorrelationId) : CorrelatedBy<Guid>;
}
