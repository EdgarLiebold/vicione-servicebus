using System.Diagnostics.Metrics;
using System.Reflection;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Monitoring;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

public sealed class ServiceBusTelemetryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-SCHEMA", "one-stable-dotnet-telemetry-identity")]
    public void PublicTelemetryIdentity_IsOneStableDotnetSource()
    {
        Assert.Equal("ViciOne.ServiceBus", ServiceBusTelemetry.Name);
        Assert.Equal(ServiceBusTelemetry.Name, ServiceBusTelemetry.MeterName);
        Assert.Equal(ServiceBusTelemetry.Name, ServiceBusTelemetry.ActivitySourceName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-API", "fixed-schema-without-parallel-telemetry-api")]
    public void PublicApi_ExposesFixedOtelActivationAndNoParallelTelemetrySystem()
    {
        Assembly product = typeof(IBus).Assembly;
        MethodInfo useInstrumentation = Assert.Single(
            typeof(InstrumentationConfigurationExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.Name == nameof(InstrumentationConfigurationExtensions.UseInstrumentation));

        ParameterInfo parameter = Assert.Single(useInstrumentation.GetParameters());
        Assert.Equal(typeof(IBusFactoryConfigurator), parameter.ParameterType);
        Assert.Equal(typeof(void), useInstrumentation.ReturnType);

        Assert.Null(product.GetType("ViciOne.ServiceBus.Monitoring.InstrumentationOptions"));
        Assert.Null(product.GetType("ViciOne.ServiceBus.MetricsContext"));
        Assert.Null(product.GetType("ViciOne.ServiceBus.MetricsContextExtensions"));
        Assert.Null(product.GetType("ViciOne.ServiceBus.DependencyInjection.IHandlerConsumerAdapter"));
        Assert.Null(product.GetType("ViciOne.ServiceBus.Logging.StartedInstrument"));
        Assert.Empty(typeof(MetricOperation).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.DoesNotContain(product.GetExportedTypes(), type =>
            type.Namespace?.StartsWith("ViciOne.ServiceBus.Monitoring.Performance", StringComparison.Ordinal) == true
            || type.Name.Contains("StatsD", StringComparison.OrdinalIgnoreCase));

        MethodInfo[] outboxOperations = typeof(LogContextInstrumentationExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name is nameof(LogContextInstrumentationExtensions.StartOutboxEnqueueInstrument)
                or nameof(LogContextInstrumentationExtensions.StartOutboxDeliveryInstrument))
            .ToArray();
        Assert.Equal(2, outboxOperations.Length);
        Assert.All(outboxOperations, method =>
            Assert.Equal([typeof(ILogContext)], method.GetParameters().Select(parameter => parameter.ParameterType)));
        Assert.DoesNotContain(
            typeof(LogContextInstrumentationExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(System.Diagnostics.Stopwatch)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-SCHEMA", "otel-messaging-instrument-identities")]
    public void MetricNames_UseTheApprovedOtelAndViciOneNamespaces()
    {
        Assert.Equal("messaging.client.sent.messages", ServiceBusTelemetry.Metrics.SentMessages);
        Assert.Equal("messaging.client.consumed.messages", ServiceBusTelemetry.Metrics.ConsumedMessages);
        Assert.Equal("messaging.client.operation.duration", ServiceBusTelemetry.Metrics.ClientOperationDuration);
        Assert.Equal("messaging.process.duration", ServiceBusTelemetry.Metrics.ProcessDuration);
        Assert.StartsWith("vicione.servicebus.", ServiceBusTelemetry.Metrics.ActiveOperations, StringComparison.Ordinal);
        Assert.StartsWith("vicione.servicebus.", ServiceBusTelemetry.Metrics.RetryAttempts, StringComparison.Ordinal);
        Assert.StartsWith("vicione.servicebus.", ServiceBusTelemetry.Metrics.DeliveryDuration, StringComparison.Ordinal);
        Assert.StartsWith("vicione.servicebus.", ServiceBusTelemetry.Metrics.OutboxMessages, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-SCHEMA", "all-supported-transport-identities-normalize-to-bounded-values")]
    public void MessagingSystemNormalizer_MapsEverySupportedTransportIdentityToTheFixedSchema()
    {
        var expected = new Dictionary<string, string>
        {
            ["activemq"] = ServiceBusTelemetry.MessagingSystems.ActiveMq,
            ["amazon_sqs"] = ServiceBusTelemetry.MessagingSystems.AmazonSqs,
            ["amazonsqs"] = ServiceBusTelemetry.MessagingSystems.AmazonSqs,
            ["aws.sns"] = ServiceBusTelemetry.MessagingSystems.AmazonSns,
            ["aws-sqs"] = ServiceBusTelemetry.MessagingSystems.AmazonSqs,
            ["aws_sqs"] = ServiceBusTelemetry.MessagingSystems.AmazonSqs,
            ["azure-service-bus"] = ServiceBusTelemetry.MessagingSystems.AzureServiceBus,
            ["db"] = ServiceBusTelemetry.MessagingSystems.Sql,
            ["eventhubs"] = ServiceBusTelemetry.MessagingSystems.AzureEventHubs,
            ["in-memory"] = ServiceBusTelemetry.MessagingSystems.InMemory,
            ["loopback"] = ServiceBusTelemetry.MessagingSystems.InMemory,
            ["rabbitmq"] = ServiceBusTelemetry.MessagingSystems.RabbitMq,
            ["sb"] = ServiceBusTelemetry.MessagingSystems.AzureServiceBus,
            ["servicebus"] = ServiceBusTelemetry.MessagingSystems.AzureServiceBus,
            ["sql"] = ServiceBusTelemetry.MessagingSystems.Sql,
            ["custom-transport"] = ServiceBusTelemetry.MessagingSystems.Other,
            [""] = ServiceBusTelemetry.MessagingSystems.Unknown,
        };

        Assert.All(expected, item =>
            Assert.Equal(item.Value, MessagingSystemNormalizerTestDriver.Normalize(item.Key)));
        Assert.Equal(ServiceBusTelemetry.MessagingSystems.Unknown,
            MessagingSystemNormalizerTestDriver.Normalize(null));
    }
}
