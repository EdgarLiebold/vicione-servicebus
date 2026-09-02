using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Runtime.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.DependencyInjection.Testing;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Monitoring;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class MessagePipelineMetricsTests
{
    private static readonly DateTimeOffset ObservationTime =
        new(2035, 4, 5, 6, 7, 8, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-INSTRUMENTS", "exact-types-units-descriptions-and-version")]
    public async Task BareContainer_PublishesTheExactInstrumentContractThroughItsOwnFactory()
    {
        TimeSpan timeout = OperationTimeout();
        await using ServiceProvider provider = CreateProvider<ObservedMessage>(static _ => Task.CompletedTask, timeout);
        IMeterFactory factory = provider.GetRequiredService<IMeterFactory>();
        using var observations = new MetricObservationSession(factory);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            string version = Assert.IsType<string>(HostMetadataCache.Host.ViciOneServiceBusVersion);
            (string Name, Type InstrumentType, string? Unit)[] expected =
                [
                    (ServiceBusTelemetry.Metrics.ActiveOperations, typeof(UpDownCounter<long>), "{operation}"),
                    (ServiceBusTelemetry.Metrics.ClientOperationDuration, typeof(Histogram<double>), "s"),
                    (ServiceBusTelemetry.Metrics.ConsumedMessages, typeof(Counter<long>), "{message}"),
                    (ServiceBusTelemetry.Metrics.DeliveryDuration, typeof(Histogram<double>), "s"),
                    (ServiceBusTelemetry.Metrics.OutboxMessages, typeof(Counter<long>), "{message}"),
                    (ServiceBusTelemetry.Metrics.ProcessDuration, typeof(Histogram<double>), "s"),
                    (ServiceBusTelemetry.Metrics.RetryAttempts, typeof(Counter<long>), "{attempt}"),
                    (ServiceBusTelemetry.Metrics.SentMessages, typeof(Counter<long>), "{message}"),
                ];
            Assert.Equal(
                expected.OrderBy(item => item.Name, StringComparer.Ordinal),
                observations.Instruments
                    .Select(item => (item.Name, item.InstrumentType, item.Unit))
                    .OrderBy(item => item.Name, StringComparer.Ordinal));
            Assert.All(observations.Instruments, instrument =>
            {
                Assert.Equal(version, instrument.SourceVersion);
                Assert.False(string.IsNullOrWhiteSpace(instrument.Description));
            });

            double[] expectedDurationBoundaries =
                [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10];
            string[] durationInstruments =
                [
                    ServiceBusTelemetry.Metrics.ClientOperationDuration,
                    ServiceBusTelemetry.Metrics.DeliveryDuration,
                    ServiceBusTelemetry.Metrics.ProcessDuration,
                ];
            Assert.All(
                observations.Instruments.Where(instrument => durationInstruments.Contains(instrument.Name)),
                instrument => Assert.Equal(expectedDurationBoundaries, instrument.HistogramBucketBoundaries));
            Assert.All(
                observations.Instruments.Where(instrument => !durationInstruments.Contains(instrument.Name)),
                instrument => Assert.Null(instrument.HistogramBucketBoundaries));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-METRICS", "successful-send-receive-process-exact-tags")]
    public async Task SuccessfulMessage_EmitsSendReceiveAndProcessMetricsWithOnlyBoundedTags()
    {
        TimeSpan timeout = OperationTimeout();
        await using ServiceProvider provider = CreateProvider<ObservedMessage>(static _ => Task.CompletedTask, timeout);
        IMeterFactory factory = provider.GetRequiredService<IMeterFactory>();
        using var observations = new MetricObservationSession(factory);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            await harness.Bus.Publish(new ObservedMessage("ok"), TestCancellationToken);
            Assert.True(await harness.Consumed.Any<ObservedMessage>(TestCancellationToken));
            await observations.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration,
                1,
                timeout,
                TestCancellationToken);

            MetricMeasurement sent = Assert.Single(observations.Measurements,
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.SentMessages);
            MetricMeasurement consumed = Assert.Single(observations.Measurements,
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ConsumedMessages);
            MetricMeasurement processed = Assert.Single(observations.Measurements,
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);

            Assert.Equal(1, sent.Value);
            Assert.Equal(1, consumed.Value);
            Assert.Equal("in-memory", sent.Tag(ServiceBusTelemetry.Attributes.MessagingSystem));
            Assert.Equal("send", sent.Tag(ServiceBusTelemetry.Attributes.OperationName));
            Assert.Equal("send", sent.Tag(ServiceBusTelemetry.Attributes.OperationType));
            Assert.Equal("in-memory", consumed.Tag(ServiceBusTelemetry.Attributes.MessagingSystem));
            Assert.Equal("receive", consumed.Tag(ServiceBusTelemetry.Attributes.OperationType));
            Assert.Equal("in-memory", processed.Tag(ServiceBusTelemetry.Attributes.MessagingSystem));
            Assert.Equal("handle", processed.Tag(ServiceBusTelemetry.Attributes.OperationName));
            Assert.Equal("process", processed.Tag(ServiceBusTelemetry.Attributes.OperationType));
            Assert.Equal("handler", processed.Tag(ServiceBusTelemetry.Attributes.ProcessorKind));

            Assert.Equal(
                [1d, 1d, 1d, -1d, -1d, -1d],
                observations.Measurements
                    .Where(item => item.Name == ServiceBusTelemetry.Metrics.ActiveOperations)
                    .Select(item => item.Value)
                    .OrderDescending());
            AssertTagKeys(sent, ServiceBusTelemetry.Attributes.MessagingSystem,
                ServiceBusTelemetry.Attributes.OperationName, ServiceBusTelemetry.Attributes.OperationType);
            AssertTagKeys(consumed, ServiceBusTelemetry.Attributes.MessagingSystem,
                ServiceBusTelemetry.Attributes.OperationName, ServiceBusTelemetry.Attributes.OperationType);
            AssertTagKeys(processed, ServiceBusTelemetry.Attributes.MessagingSystem,
                ServiceBusTelemetry.Attributes.OperationName, ServiceBusTelemetry.Attributes.OperationType,
                ServiceBusTelemetry.Attributes.ProcessorKind);

            string[] forbiddenFragments = ["message.type", "consumer", "destination", "address", "payload", "custom"];
            Assert.DoesNotContain(observations.Measurements.SelectMany(item => item.Tags), tag =>
                forbiddenFragments.Any(fragment => tag.Key.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-METRICS", "processor-kind-uses-explicit-semantic-contract")]
    public async Task ConsumerClassification_UsesTheExplicitAdapterContractInsteadOfTypeNames()
    {
        TimeSpan timeout = OperationTimeout();
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<MessageHandlerConsumerLookalike>();
                configuration.AddHandler<ActualHandlerMessage>(static (ConsumeContext<ActualHandlerMessage> _) => Task.CompletedTask);
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            await harness.Bus.Publish(new OrdinaryConsumerMessage("consumer"), TestCancellationToken);
            await harness.Bus.Publish(new ActualHandlerMessage("handler"), TestCancellationToken);
            await observations.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration,
                2,
                timeout,
                TestCancellationToken);

            Assert.Equal(
                [("consume", "consumer"), ("handle", "handler")],
                observations.Measurements
                    .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration)
                    .Select(measurement => (
                        Assert.IsType<string>(measurement.Tag(ServiceBusTelemetry.Attributes.OperationName)),
                        Assert.IsType<string>(measurement.Tag(ServiceBusTelemetry.Attributes.ProcessorKind))))
                    .OrderBy(item => item.Item1, StringComparer.Ordinal));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-METRICS", "fault-uses-fully-qualified-error-type")]
    public async Task FaultedHandler_RecordsTheFullyQualifiedErrorTypeOnItsDuration()
    {
        TimeSpan timeout = OperationTimeout();
        await using ServiceProvider provider = CreateProvider<FaultedMessage>(
            static _ => Task.FromException(new ExpectedHandlerException()),
            timeout);
        IMeterFactory factory = provider.GetRequiredService<IMeterFactory>();
        using var observations = new MetricObservationSession(factory);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            await harness.Bus.Publish(new FaultedMessage("fault"), TestCancellationToken);
            Assert.True(await harness.Consumed.Any<FaultedMessage>(TestCancellationToken));
            await observations.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration
                    && Equals(measurement.Tag(ServiceBusTelemetry.Attributes.ErrorType), typeof(ExpectedHandlerException).FullName),
                1,
                timeout,
                TestCancellationToken);

            MetricMeasurement fault = Assert.Single(observations.Measurements, measurement =>
                measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration
                && measurement.Tags.Any(tag => tag.Key == ServiceBusTelemetry.Attributes.ErrorType));
            Assert.Equal(typeof(ExpectedHandlerException).FullName,
                fault.Tag(ServiceBusTelemetry.Attributes.ErrorType));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-METRICS", "failed-send-remains-an-attempt-with-exact-error-type")]
    public async Task FaultedSend_RecordsTheAttemptAndItsFullyQualifiedErrorType()
    {
        TimeSpan timeout = OperationTimeout();
        await using ServiceProvider provider = CreateProvider<ObservedMessage>(static _ => Task.CompletedTask, timeout);
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            ISendEndpoint endpoint = await harness.GetHandlerEndpoint<ObservedMessage>().WaitAsync(timeout, TestCancellationToken);
            await Assert.ThrowsAsync<SerializationException>(() => endpoint.Send(
                new ObservedMessage("faulted-send"),
                context => context.Serializer = null!,
                TestCancellationToken));
            await observations.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.SentMessages,
                1,
                timeout,
                TestCancellationToken);

            MetricMeasurement attempt = Assert.Single(observations.Measurements,
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.SentMessages);
            Assert.Equal(1, attempt.Value);
            Assert.Equal(typeof(SerializationException).FullName,
                attempt.Tag(ServiceBusTelemetry.Attributes.ErrorType));
            AssertTagKeys(attempt,
                ServiceBusTelemetry.Attributes.ErrorType,
                ServiceBusTelemetry.Attributes.MessagingSystem,
                ServiceBusTelemetry.Attributes.OperationName,
                ServiceBusTelemetry.Attributes.OperationType);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-METRICS", "retry-counted-on-the-retried-attempt")]
    public async Task RetriedHandler_EmitsOneRetryMeasurementAndTwoProcessingDurations()
    {
        TimeSpan timeout = OperationTimeout();
        var attempts = new AttemptCounter();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(attempts)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddHandler<RetryMessage>((ConsumeContext<RetryMessage> _) =>
                    attempts.Increment() == 1
                        ? Task.FromException(new ExpectedRetryException())
                        : Task.CompletedTask);
                configuration.AddConfigureEndpointsCallback((_, endpoint) =>
                    endpoint.UseMessageRetry(retry => retry.Immediate(1)));
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        IMeterFactory factory = provider.GetRequiredService<IMeterFactory>();
        using var observations = new MetricObservationSession(factory);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            await harness.Bus.Publish(new RetryMessage("retry"), TestCancellationToken);
            Assert.True(await harness.Consumed.Any<RetryMessage>(TestCancellationToken));
            await observations.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration,
                2,
                timeout,
                TestCancellationToken);

            MetricMeasurement retry = Assert.Single(observations.Measurements,
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.RetryAttempts);
            Assert.Equal(1, retry.Value);
            Assert.Equal(2, attempts.Count);
            Assert.Equal(2, observations.Measurements.Count(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-TIME", "duration-uses-context-time-provider")]
    public async Task ProcessingDuration_UsesTheContextTimeProviderInsteadOfWallClockTime()
    {
        TimeSpan timeout = OperationTimeout();
        var timeProvider = new FakeTimeProvider(ObservationTime);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<TimeProvider>(timeProvider)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddHandler<TimedMessage>((ConsumeContext<TimedMessage> _) =>
                {
                    timeProvider.Advance(TimeSpan.FromSeconds(7));
                    return Task.CompletedTask;
                });
                configuration.AddConfigureEndpointsCallback((_, endpoint) =>
                    endpoint.UseExecute(context => context.SetTimeProvider(timeProvider)));
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        IMeterFactory factory = provider.GetRequiredService<IMeterFactory>();
        using var observations = new MetricObservationSession(factory);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            await harness.Bus.Publish(new TimedMessage("time"), TestCancellationToken);
            Assert.True(await harness.Consumed.Any<TimedMessage>(TestCancellationToken));
            await observations.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration,
                1,
                timeout,
                TestCancellationToken);

            MetricMeasurement duration = Assert.Single(observations.Measurements,
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
            Assert.Equal(7, duration.Value);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-METRICS", "active-processing-brackets-the-real-operation-lifetime")]
    public async Task ActiveOperations_BracketTheActualProcessingLifetime()
    {
        TimeSpan timeout = OperationTimeout();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = CreateProvider<GatedMessage>(async context =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(timeout, context.CancellationToken);
        }, timeout);
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            await harness.Bus.Publish(new GatedMessage("gated"), TestCancellationToken);
            await entered.Task.WaitAsync(timeout, TestCancellationToken);
            await observations.WaitForCountAsync(IsProcessActivity, 1, timeout, TestCancellationToken);

            Assert.Equal([1d], observations.Measurements.Where(IsProcessActivity).Select(item => item.Value));

            release.TrySetResult();
            Assert.True(await harness.Consumed.Any<GatedMessage>(TestCancellationToken));
            await observations.WaitForCountAsync(IsProcessActivity, 2, timeout, TestCancellationToken);

            Assert.Equal([1d, -1d], observations.Measurements.Where(IsProcessActivity).Select(item => item.Value));
        }
        finally
        {
            release.TrySetResult();
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        static bool IsProcessActivity(MetricMeasurement measurement) =>
            measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations
            && Equals(measurement.Tag(ServiceBusTelemetry.Attributes.OperationType), "process");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "throwing-listener-cannot-change-message-flow")]
    public async Task ThrowingMeterListener_CannotChangeSuccessfulMessageDelivery()
    {
        TimeSpan timeout = OperationTimeout();
        await using ServiceProvider provider = CreateProvider<ObservedMessage>(static _ => Task.CompletedTask, timeout);
        IMeterFactory factory = provider.GetRequiredService<IMeterFactory>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, current) =>
        {
            if (instrument.Meter.Name == ServiceBusTelemetry.MeterName
                && ReferenceEquals(instrument.Meter.Scope, factory))
                current.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>(static (_, _, _, _) => throw new ExpectedListenerException());
        listener.SetMeasurementEventCallback<double>(static (_, _, _, _) => throw new ExpectedListenerException());
        listener.Start();
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            await harness.Bus.Publish(new ObservedMessage("still-delivered"), TestCancellationToken);
            IReceivedMessage<ObservedMessage> consumed = await harness.Consumed
                .SelectAsync<ObservedMessage>(TestCancellationToken)
                .First()
                .WaitAsync(timeout, TestCancellationToken);

            Assert.Equal("still-delivered", consumed.Context.Message.Value);
            Assert.Null(consumed.Exception);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-DI", "application-meter-factory-preserved-and-failure-contained")]
    public async Task ApplicationMeterFactory_IsPreservedAndItsFailureCannotPreventDelivery()
    {
        TimeSpan timeout = OperationTimeout();
        var factory = new ThrowingMeterFactory();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IMeterFactory>(factory)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddHandler<ObservedMessage>(static (ConsumeContext<ObservedMessage> _) => Task.CompletedTask);
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.Same(factory, provider.GetRequiredService<IMeterFactory>());
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);
        try
        {
            await harness.Bus.Publish(new ObservedMessage("factory-failed"), TestCancellationToken);
            Assert.True(await harness.Consumed.Any<ObservedMessage>(TestCancellationToken));
            Assert.True(factory.CreateAttempts > 0);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "failed-factory-cannot-reuse-prior-provider-scope")]
    public async Task FailedMeterFactory_CannotLeakMeasurementsIntoAnEarlierProvider()
    {
        TimeSpan timeout = OperationTimeout();
        await using ServiceProvider healthyProvider = CreateProvider<ProviderAMessage>(static _ => Task.CompletedTask, timeout);
        using var healthyObservations = new MetricObservationSession(healthyProvider.GetRequiredService<IMeterFactory>());
        ITestHarness healthyHarness = await healthyProvider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        var failedFactory = new ThrowingMeterFactory();
        await using ServiceProvider failedProvider = new ServiceCollection()
            .AddSingleton<IMeterFactory>(failedFactory)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddHandler<ProviderBMessage>(static (ConsumeContext<ProviderBMessage> _) => Task.CompletedTask);
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness failedHarness = await failedProvider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            await healthyHarness.Bus.Publish(new ProviderAMessage("healthy"), TestCancellationToken);
            Assert.True(await healthyHarness.Consumed.Any<ProviderAMessage>(TestCancellationToken));
            await healthyObservations.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.SentMessages,
                1,
                timeout,
                TestCancellationToken);

            await failedHarness.Bus.Publish(new ProviderBMessage("unobserved"), TestCancellationToken);
            Assert.True(await failedHarness.Consumed.Any<ProviderBMessage>(TestCancellationToken));

            Assert.Equal(1, healthyObservations.Measurements.Count(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.SentMessages));
            Assert.True(failedFactory.CreateAttempts > 0);
        }
        finally
        {
            await failedHarness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            await healthyHarness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-DI", "independent-providers-have-independent-meter-scopes")]
    public async Task IndependentServiceProviders_EmitOnlyThroughTheirOwnMeterFactories()
    {
        TimeSpan timeout = OperationTimeout();
        await using ServiceProvider providerA = CreateProvider<ProviderAMessage>(static _ => Task.CompletedTask, timeout);
        await using ServiceProvider providerB = CreateProvider<ProviderBMessage>(static _ => Task.CompletedTask, timeout);
        using var observationsA = new MetricObservationSession(providerA.GetRequiredService<IMeterFactory>());
        using var observationsB = new MetricObservationSession(providerB.GetRequiredService<IMeterFactory>());
        ITestHarness harnessA = await providerA.StartTestHarness().WaitAsync(timeout, TestCancellationToken);
        ITestHarness harnessB = await providerB.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            await harnessA.Bus.Publish(new ProviderAMessage("a"), TestCancellationToken);
            Assert.True(await harnessA.Consumed.Any<ProviderAMessage>(TestCancellationToken));
            await observationsA.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration,
                1,
                timeout,
                TestCancellationToken);
            Assert.Empty(observationsB.Measurements);

            await harnessB.Bus.Publish(new ProviderBMessage("b"), TestCancellationToken);
            Assert.True(await harnessB.Consumed.Any<ProviderBMessage>(TestCancellationToken));
            await observationsB.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration,
                1,
                timeout,
                TestCancellationToken);
            Assert.NotEmpty(observationsA.Measurements);
            Assert.NotEmpty(observationsB.Measurements);
        }
        finally
        {
            await harnessB.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            await harnessA.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "disposing-one-provider-does-not-disable-another")]
    public async Task DisposingOneProvider_DoesNotDisableAnotherProvidersMetrics()
    {
        TimeSpan timeout = OperationTimeout();
        ServiceProvider providerA = CreateProvider<ProviderAMessage>(static _ => Task.CompletedTask, timeout);
        await using ServiceProvider providerB = CreateProvider<ProviderBMessage>(static _ => Task.CompletedTask, timeout);
        using var observationsB = new MetricObservationSession(providerB.GetRequiredService<IMeterFactory>());
        ITestHarness harnessA = await providerA.StartTestHarness().WaitAsync(timeout, TestCancellationToken);
        ITestHarness harnessB = await providerB.StartTestHarness().WaitAsync(timeout, TestCancellationToken);
        bool providerADisposed = false;

        try
        {
            await harnessA.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            await providerA.DisposeAsync();
            providerADisposed = true;

            await harnessB.Bus.Publish(new ProviderBMessage("still-observed"), TestCancellationToken);
            Assert.True(await harnessB.Consumed.Any<ProviderBMessage>(TestCancellationToken));
            await observationsB.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration,
                1,
                timeout,
                TestCancellationToken);

            Assert.Single(observationsB.Measurements,
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
        }
        finally
        {
            if (!providerADisposed)
            {
                await harnessA.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
                await providerA.DisposeAsync();
            }

            await harnessB.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-DI", "multiple-buses-share-provider-meter-factory")]
    public async Task MultipleBusesInOneProvider_EmitThroughTheSameMeterFactory()
    {
        TimeSpan timeout = OperationTimeout();
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration => configuration.SetTestTimeouts(timeout, timeout))
            .AddViciOneServiceBus<IObservedBus>(configuration => configuration.UsingInMemory((_, bus) =>
                bus.Host(new Uri("loopback://localhost/observability-secondary"))))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            await harness.Bus.Publish(new ObservedMessage("default"), TestCancellationToken);
            await provider.GetRequiredService<IObservedBus>().Publish(
                new ObservedMessage("secondary"),
                TestCancellationToken);
            await observations.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.SentMessages,
                2,
                timeout,
                TestCancellationToken);

            Assert.Equal(2, observations.Measurements.Count(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.SentMessages));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-DI", "registration-is-idempotent-across-bus-entry-points")]
    public void MultipleBusRegistrations_AddExactlyOneMeterFactory()
    {
        IServiceCollection services = new ServiceCollection()
            .AddViciOneServiceBusTestHarness()
            .AddViciOneServiceBus<IObservedBus>(configuration => configuration.UsingInMemory((_, bus) =>
                bus.Host(new Uri("loopback://localhost/observability-registration"))));

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IMeterFactory));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-NON-DI", "explicit-activation-emits-through-process-meter")]
    public async Task ExplicitNonDiActivation_EmitsThroughItsOwnProcessMeter()
    {
        TimeSpan timeout = OperationTimeout();
        ILogContext? previous = LogContext.Current;
        using var observations = new MetricObservationSession(meterScope: null);
        IBusControl? bus = null;

        try
        {
            bus = Bus.Factory.CreateUsingInMemory(configuration =>
            {
                configuration.UseInstrumentation();
                configuration.ReceiveEndpoint("non-di-observability", endpoint =>
                    endpoint.Handler<NonDiMessage>(static _ => Task.CompletedTask));
            });
            await bus.StartAsync(TestCancellationToken).WaitAsync(timeout, TestCancellationToken);

            await bus.Publish(new NonDiMessage("explicit"), TestCancellationToken);
            await observations.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration,
                1,
                timeout,
                TestCancellationToken);

            MetricMeasurement processed = Assert.Single(observations.Measurements,
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
            Assert.Equal("handle", processed.Tag(ServiceBusTelemetry.Attributes.OperationName));
            Assert.Equal("handler", processed.Tag(ServiceBusTelemetry.Attributes.ProcessorKind));
        }
        finally
        {
            if (bus is not null)
                await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            LogContext.Current = previous!;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "di-and-explicit-non-di-meter-scopes-remain-independent")]
    public async Task DependencyInjectionAndExplicitActivation_UseIndependentMeterScopes()
    {
        TimeSpan timeout = OperationTimeout();
        ILogContext? previous = LogContext.Current;
        await using ServiceProvider provider = CreateProvider<ProviderAMessage>(static _ => Task.CompletedTask, timeout);
        using var providerObservations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        using var processObservations = new MetricObservationSession(meterScope: null);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);
        IBusControl? explicitBus = null;

        try
        {
            explicitBus = Bus.Factory.CreateUsingInMemory(configuration =>
            {
                configuration.UseInstrumentation();
                configuration.ReceiveEndpoint("mixed-observability", endpoint =>
                    endpoint.Handler<NonDiMessage>(static _ => Task.CompletedTask));
            });
            await explicitBus.StartAsync(TestCancellationToken).WaitAsync(timeout, TestCancellationToken);

            await harness.Bus.Publish(new ProviderAMessage("provider"), TestCancellationToken);
            Assert.True(await harness.Consumed.Any<ProviderAMessage>(TestCancellationToken));
            await providerObservations.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration,
                1,
                timeout,
                TestCancellationToken);
            Assert.Empty(processObservations.Measurements);

            await explicitBus.Publish(new NonDiMessage("process"), TestCancellationToken);
            await processObservations.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration,
                1,
                timeout,
                TestCancellationToken);

            Assert.Equal(1, providerObservations.Measurements.Count(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration));
            Assert.Single(processObservations.Measurements,
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
        }
        finally
        {
            if (explicitBus is not null)
                await explicitBus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            LogContext.Current = previous!;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "application-log-context-remains-unbound-to-provider-scopes")]
    public async Task ApplicationOwnedLogContext_IsNeverBoundToAProviderMeterScope()
    {
        TimeSpan timeout = OperationTimeout();
        ILogContext? previous = LogContext.Current;
        LogContext.ConfigureCurrentLogContext(NullLoggerFactory.Instance);
        ILogContext applicationLogContext = LogContext.Current;
        await using ServiceProvider providerA = CreateProvider<ProviderAMessage>(static _ => Task.CompletedTask, timeout);
        await using ServiceProvider providerB = CreateProvider<ProviderBMessage>(static _ => Task.CompletedTask, timeout);
        using var observationsA = new MetricObservationSession(providerA.GetRequiredService<IMeterFactory>());
        using var observationsB = new MetricObservationSession(providerB.GetRequiredService<IMeterFactory>());
        ITestHarness harnessA = await providerA.StartTestHarness().WaitAsync(timeout, TestCancellationToken);
        ITestHarness harnessB = await providerB.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            LogContext.Current = applicationLogContext;
            var instrument = applicationLogContext.StartOutboxDeliveryInstrument();
            instrument?.Complete();

            Assert.Empty(observationsA.Measurements);
            Assert.Empty(observationsB.Measurements);
        }
        finally
        {
            await harnessB.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            await harnessA.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            LogContext.Current = previous!;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-NON-DI", "unbound-log-context-cannot-reach-fallback-meter")]
    public async Task UnboundLogContext_CannotReachTheExplicitNonDiMeter()
    {
        TimeSpan timeout = OperationTimeout();
        ILogContext? previous = LogContext.Current;
        IBusControl? bus = null;
        var handled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            _ = Bus.Factory.CreateUsingInMemory(configuration => configuration.UseInstrumentation());
            LogContext.ConfigureCurrentLogContext(NullLoggerFactory.Instance);
            using var observations = new MetricObservationSession(meterScope: null);
            bus = Bus.Factory.CreateUsingInMemory(configuration =>
                configuration.ReceiveEndpoint("unbound-observability", endpoint =>
                    endpoint.Handler<UnboundMessage>(_ =>
                    {
                        handled.TrySetResult();
                        return Task.CompletedTask;
                    })));
            await bus.StartAsync(TestCancellationToken).WaitAsync(timeout, TestCancellationToken);

            await bus.Publish(new UnboundMessage("unbound"), TestCancellationToken);
            await handled.Task.WaitAsync(timeout, TestCancellationToken);

            Assert.Empty(observations.Measurements);
        }
        finally
        {
            if (bus is not null)
                await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            LogContext.Current = previous!;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-OUTBOX", "metric-operation-is-idempotent-and-first-failure-wins")]
    public async Task MetricOperation_CompletesOnceAndPreservesTheFirstObservedException()
    {
        TimeSpan timeout = OperationTimeout();
        var entered = new TaskCompletionSource<ILogContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = CreateProvider<ObservedMessage>(_ =>
        {
            entered.TrySetResult(LogContext.Current);
            return Task.CompletedTask;
        }, timeout);
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            await harness.Bus.Publish(new ObservedMessage("capture-context"), TestCancellationToken);
            ILogContext logContext = await entered.Task.WaitAsync(timeout, TestCancellationToken);
            OutboxTelemetryTestDriver.RecordDeliveryTwice(
                logContext,
                new ExpectedHandlerException(),
                new ExpectedRetryException());

            MetricMeasurement delivery = Assert.Single(observations.Measurements,
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.OutboxMessages);
            Assert.Equal("faulted", delivery.Tag(ServiceBusTelemetry.Attributes.Outcome));
            Assert.Equal(typeof(ExpectedHandlerException).FullName,
                delivery.Tag(ServiceBusTelemetry.Attributes.ErrorType));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-OUTBOX", "enqueue-and-deliver-have-distinct-bounded-outcomes")]
    public async Task InMemoryOutbox_EmitsDistinctEnqueueAndDeliveryOutcomes()
    {
        TimeSpan timeout = OperationTimeout();
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddHandler<OutboxResult>(static (ConsumeContext<OutboxResult> _) => Task.CompletedTask);
                configuration.AddConfigureEndpointsCallback((_, endpoint) => endpoint.UseInMemoryOutbox());
                configuration.AddHandler<OutboxRequest>((ConsumeContext<OutboxRequest> context) =>
                    context.Publish(new OutboxResult(context.Message.Value), context.CancellationToken));
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            await harness.Bus.Publish(new OutboxRequest("deferred"), TestCancellationToken);
            Assert.True(await harness.Consumed.Any<OutboxResult>(TestCancellationToken));
            await observations.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.OutboxMessages,
                2,
                timeout,
                TestCancellationToken);

            MetricMeasurement[] outbox = observations.Measurements
                .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.OutboxMessages)
                .ToArray();
            Assert.Equal(2, outbox.Length);
            Assert.Equal(["deliver", "enqueue"], outbox
                .Select(item => Assert.IsType<string>(item.Tag(ServiceBusTelemetry.Attributes.OutboxOperation)))
                .Order(StringComparer.Ordinal));
            Assert.All(outbox, item =>
            {
                Assert.Equal("succeeded", item.Tag(ServiceBusTelemetry.Attributes.Outcome));
                AssertTagKeys(item, ServiceBusTelemetry.Attributes.OutboxOperation,
                    ServiceBusTelemetry.Attributes.Outcome);
            });
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-OUTBOX", "delivery-failure-does-not-rewrite-successful-enqueue")]
    public async Task InMemoryOutbox_DeliveryFailurePreservesTheSuccessfulEnqueueOutcome()
    {
        TimeSpan timeout = OperationTimeout();
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConfigureEndpointsCallback((_, endpoint) => endpoint.UseInMemoryOutbox());
                configuration.AddHandler<OutboxResult>(static (ConsumeContext<OutboxResult> _) => Task.CompletedTask);
                configuration.AddHandler<OutboxFaultRequest>((ConsumeContext<OutboxFaultRequest> context) =>
                    context.Publish(
                        new OutboxResult(context.Message.Value),
                        sendContext => sendContext.Serializer = null!,
                        context.CancellationToken));
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, TestCancellationToken);

        try
        {
            await harness.Bus.Publish(new OutboxFaultRequest("faulted-delivery"), TestCancellationToken);
            await observations.WaitForCountAsync(
                measurement => measurement.Name == ServiceBusTelemetry.Metrics.OutboxMessages,
                2,
                timeout,
                TestCancellationToken);

            MetricMeasurement enqueue = Assert.Single(observations.Measurements, measurement =>
                measurement.Name == ServiceBusTelemetry.Metrics.OutboxMessages
                && Equals(measurement.Tag(ServiceBusTelemetry.Attributes.OutboxOperation), "enqueue"));
            MetricMeasurement delivery = Assert.Single(observations.Measurements, measurement =>
                measurement.Name == ServiceBusTelemetry.Metrics.OutboxMessages
                && Equals(measurement.Tag(ServiceBusTelemetry.Attributes.OutboxOperation), "deliver"));

            Assert.Equal("succeeded", enqueue.Tag(ServiceBusTelemetry.Attributes.Outcome));
            AssertTagKeys(enqueue,
                ServiceBusTelemetry.Attributes.OutboxOperation,
                ServiceBusTelemetry.Attributes.Outcome);
            Assert.Equal("faulted", delivery.Tag(ServiceBusTelemetry.Attributes.Outcome));
            Assert.Equal(typeof(SerializationException).FullName,
                delivery.Tag(ServiceBusTelemetry.Attributes.ErrorType));
            AssertTagKeys(delivery,
                ServiceBusTelemetry.Attributes.ErrorType,
                ServiceBusTelemetry.Attributes.OutboxOperation,
                ServiceBusTelemetry.Attributes.Outcome);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static ServiceProvider CreateProvider<TMessage>(
        Func<ConsumeContext<TMessage>, Task> handler,
        TimeSpan timeout)
        where TMessage : class =>
        new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddHandler<TMessage>(handler);
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private static void AssertTagKeys(MetricMeasurement measurement, params string[] expected) =>
        Assert.Equal(expected.Order(StringComparer.Ordinal),
            measurement.Tags.Select(tag => tag.Key).Order(StringComparer.Ordinal));

    public sealed record ObservedMessage(string Value);
    public sealed record FaultedMessage(string Value);
    public sealed record RetryMessage(string Value);
    public sealed record TimedMessage(string Value);
    public sealed record GatedMessage(string Value);
    public sealed record ProviderAMessage(string Value);
    public sealed record ProviderBMessage(string Value);
    public sealed record NonDiMessage(string Value);
    public sealed record UnboundMessage(string Value);
    public sealed record OutboxRequest(string Value);
    public sealed record OutboxFaultRequest(string Value);
    public sealed record OutboxResult(string Value);
    public sealed record OrdinaryConsumerMessage(string Value);
    public sealed record ActualHandlerMessage(string Value);

    public sealed class MessageHandlerConsumerLookalike : IConsumer<OrdinaryConsumerMessage>
    {
        public Task Consume(ConsumeContext<OrdinaryConsumerMessage> context) => Task.CompletedTask;
    }

    private sealed class AttemptCounter
    {
        private int _count;
        public int Count => Volatile.Read(ref _count);
        public int Increment() => Interlocked.Increment(ref _count);
    }

    private sealed class ThrowingMeterFactory : IMeterFactory
    {
        private int _createAttempts;
        public int CreateAttempts => Volatile.Read(ref _createAttempts);

        public Meter Create(MeterOptions options)
        {
            Interlocked.Increment(ref _createAttempts);
            throw new ExpectedMeterFactoryException();
        }

        public void Dispose()
        {
        }
    }

    private sealed class ExpectedHandlerException : Exception
    {
    }

    private sealed class ExpectedRetryException : Exception
    {
    }

    private sealed class ExpectedListenerException : Exception
    {
    }

    private sealed class ExpectedMeterFactoryException : Exception
    {
    }
}

public interface IObservedBus : IBus
{
}
