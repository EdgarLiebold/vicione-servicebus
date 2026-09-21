using ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests;

public sealed class AmazonSqsDirectFactoryLimitsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-DIRECT-LIMITS", "missing-limits-fail-before-bus-build")]
    public void DirectFactory_RejectsMissingLimitsBeforeBusBuild()
    {
        ConfigurationException failure = Assert.Throws<ConfigurationException>(
            () => Bus.Factory.CreateUsingAmazonSqs(_ => { }));

        Assert.Contains("Message limits", failure.Message, StringComparison.Ordinal);
        Assert.Contains("Limits", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-DIRECT-LIMITS", "null-configurator-is-rejected")]
    public void Limits_RejectsNullFactoryConfigurator()
    {
        ArgumentNullException failure = Assert.Throws<ArgumentNullException>(
            () => MessageLimitsConfigurationExtensions.Limits<IBusFactoryConfigurator>(null!, MessageLimits.Conservative));

        Assert.Equal("configurator", failure.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-DIRECT-LIMITS", "null-limits-are-rejected")]
    public void Limits_RejectsNullPolicy()
    {
        ArgumentNullException failure = Assert.Throws<ArgumentNullException>(
            () => Bus.Factory.CreateUsingAmazonSqs(configurator => configurator.Limits(null!)));

        Assert.Equal("limits", failure.ParamName);
    }

    [Theory]
    [InlineData(0, 1, 16, "MaxBodyBytes")]
    [InlineData(64, 63, 16, "MaxEnvelopeBytes")]
    [InlineData(64, 128, 0, "MaxJsonDepth")]
    [RequirementCoverage("REQ-VSB-AWS-SQS-DIRECT-LIMITS", "invalid-body-envelope-and-depth-fail-during-configuration")]
    public void Limits_RejectsInvalidBoundariesBeforeBusBuild(
        int maxBodyBytes,
        int maxEnvelopeBytes,
        int maxJsonDepth,
        string property)
    {
        var limits = new MessageLimits
        {
            MaxBodyBytes = maxBodyBytes,
            MaxEnvelopeBytes = maxEnvelopeBytes,
            MaxJsonDepth = maxJsonDepth,
        };

        ConfigurationException failure = Assert.Throws<ConfigurationException>(
            () => Bus.Factory.CreateUsingAmazonSqs(configurator => configurator.Limits(limits)));

        Assert.Contains(property, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-DIRECT-LIMITS", "duplicate-policy-owner-is-rejected")]
    public void Limits_RejectsDuplicateDeclaration()
    {
        ConfigurationException failure = Assert.Throws<ConfigurationException>(
            () => Bus.Factory.CreateUsingAmazonSqs(configurator =>
            {
                configurator.Limits(MessageLimits.Conservative);
                configurator.Limits(MessageLimits.Conservative);
            }));

        Assert.Contains("already declared", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-DIRECT-LIMITS", "typed-return-and-json-depth-are-bound-to-direct-factory")]
    public async Task Limits_PreservesFactoryTypeAndAppliesJsonDepthAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("factorydepth");
        var limits = new MessageLimits
        {
            MaxBodyBytes = 64 * 1024,
            MaxEnvelopeBytes = 128 * 1024,
            MaxJsonDepth = 3,
        };
        IAmazonSqsBusFactoryConfigurator? returned = null;

        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            returned = configurator.Limits(limits);
            Assert.Same(configurator, returned);
            configurator.ConfigureJsonSerializerOptions(options =>
            {
                options.MaxDepth = 64;
                return options;
            });
            fixture.ConfigureRegisteredHost(configurator);
        });
        Assert.NotNull(returned);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(
                    new Uri($"queue:{fixture.Name("depth-target")}"),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            System.Runtime.Serialization.SerializationException failure =
                await Assert.ThrowsAsync<System.Runtime.Serialization.SerializationException>(
                () => endpoint.SendAsync(CreateDepth(4), cancellationToken));
            Assert.IsType<System.Text.Json.JsonException>(failure.InnerException);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-DIRECT-LIMITS", "direct-factory-enforces-transport-envelope-limit")]
    public async Task DirectFactory_EnforcesTransportEnvelopeLimitAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("factoryenvelope");
        var limits = new MessageLimits
        {
            MaxBodyBytes = 256,
            MaxEnvelopeBytes = 256,
            MaxJsonDepth = 32,
        };
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator => fixture.ConfigureHost(configurator, limits));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(
                    new Uri($"queue:{fixture.Name("envelope-target")}"),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            PayloadAdmissionException failure = await Assert.ThrowsAsync<PayloadAdmissionException>(
                () => endpoint.SendAsync(new LimitProbe("envelope-rejection"), cancellationToken));

            Assert.Equal(PayloadAdmissionStage.TransportEnvelope, failure.Stage);
            Assert.Equal((long)limits.MaxEnvelopeBytes, failure.ConfiguredLimitBytes);
            Assert.True(failure.ActualBytes > failure.ConfiguredLimitBytes);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-DIRECT-LIMITS", "two-direct-buses-enforce-distinct-body-limits")]
    public async Task DirectFactories_EnforceIndependentBodyLimitsAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("factorylimits");
        string queueName = fixture.Name("input");
        var received = new TaskCompletionSource<LimitProbe>(TaskCreationOptions.RunContinuationsAsynchronously);
        var smallLimits = new MessageLimits
        {
            MaxBodyBytes = 128,
            MaxEnvelopeBytes = 1024,
            MaxJsonDepth = 32,
        };
        IBusControl smallBus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
            fixture.ConfigureHost(configurator, smallLimits));
        IBusControl largeBus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator, MessageLimits.Conservative);
            configurator.ReceiveEndpoint(queueName, endpoint =>
                endpoint.Handler<LimitProbe>(context =>
                {
                    received.TrySetResult(context.Message);
                    return Task.CompletedTask;
                }));
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool smallStarted = false;
        bool largeStarted = false;

        try
        {
            await largeBus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            largeStarted = true;
            await smallBus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            smallStarted = true;

            Uri address = new($"queue:{queueName}");
            ISendEndpoint smallEndpoint = await smallBus.GetSendEndpointAsync(address, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ISendEndpoint largeEndpoint = await largeBus.GetSendEndpointAsync(address, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            var message = new LimitProbe(new string('x', 256));

            PayloadAdmissionException failure = await Assert.ThrowsAsync<PayloadAdmissionException>(
                () => smallEndpoint.SendAsync(message, cancellationToken));
            Assert.Equal(PayloadAdmissionStage.SerializedBody, failure.Stage);
            Assert.Equal(smallLimits.MaxBodyBytes, failure.ConfiguredLimitBytes);
            Assert.True(failure.ActualBytes > failure.ConfiguredLimitBytes);

            await largeEndpoint.SendAsync(message, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(message, await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
        }
        finally
        {
            if (smallStarted)
                await smallBus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            if (largeStarted)
                await largeBus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-DIRECT-LIMITS", "direct-factory-enforces-message-data-threshold")]
    public async Task DirectFactory_RequiresMessageDataAboveDeclaredThresholdAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("factoryoffload");
        var limits = new MessageLimits
        {
            MaxBodyBytes = 1024 * 1024,
            MaxEnvelopeBytes = 2 * 1024 * 1024,
            MaxJsonDepth = 32,
            OffloadToMessageDataAboveBytes = 64,
        };
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator => fixture.ConfigureHost(configurator, limits));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{fixture.Name("target")}"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            PayloadAdmissionException failure = await Assert.ThrowsAsync<PayloadAdmissionException>(
                () => endpoint.SendAsync(new LimitProbe(new string('x', 128)), cancellationToken));

            Assert.Equal(PayloadAdmissionStage.MessageData, failure.Stage);
            Assert.Equal((long)limits.OffloadToMessageDataAboveBytes!.Value, failure.ConfiguredLimitBytes);
            Assert.True(failure.ActualBytes > failure.ConfiguredLimitBytes);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    public sealed record LimitProbe(string Value);

    public sealed class DepthProbe
    {
        public DepthProbe(DepthProbe? child = null)
        {
            Child = child;
        }

        public DepthProbe? Child { get; }
    }

    private static DepthProbe CreateDepth(int depth) =>
        depth == 0 ? new DepthProbe() : new DepthProbe(CreateDepth(depth - 1));
}
