using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class TechnicalRetryArchitectureTests
{
    private static readonly IReadOnlyDictionary<string, (int Immediate, int Delayed)> CanonicalPolicyOwners =
        new Dictionary<string, (int Immediate, int Delayed)>(StringComparer.Ordinal)
        {
            ["src/ViciOne.ServiceBus/DependencyInjection/Registration/Futures/DefaultFutureDefinition.cs"] = (1, 1),
            ["src/ViciOne.ServiceBus/DependencyInjection/Registration/Futures/RequestConsumerFutureDefinition.cs"] = (1, 0),
            ["src/ViciOne.ServiceBus/JobService/Configuration/JobAttemptSagaDefinition.cs"] = (1, 0),
            ["src/ViciOne.ServiceBus/JobService/Configuration/JobSagaDefinition.cs"] = (1, 0),
            ["src/ViciOne.ServiceBus/JobService/Configuration/JobServiceConfigurator.cs"] = (3, 0),
            ["src/ViciOne.ServiceBus/JobService/Configuration/JobTypeSagaDefinition.cs"] = (1, 0),
            ["src/Scheduling/ViciOne.ServiceBus.Quartz/Configuration/ScheduleMessageConsumerDefinition.cs"] = (1, 0),
        };

    [Fact]
    [RequirementCoverage("REQ-VSB-TECHNICAL-RETRY-ARCHITECTURE", "internal-runtime-defaults-use-one-canonical-policy")]
    public void InternalRuntimeDefaults_UseOnlyTheCanonicalTechnicalRetryPolicy()
    {
        foreach ((string relativePath, (int immediate, int delayed)) in CanonicalPolicyOwners)
        {
            string source = File.ReadAllText(Path.Combine(RepositoryLayout.Root, relativePath));

            Assert.Equal(immediate, Count(source, ".UseTechnicalMessageRetry();"));
            Assert.Equal(delayed, Count(source, ".UseTechnicalDelayedRedelivery();"));
            Assert.DoesNotContain(".UseMessageRetry(", source, StringComparison.Ordinal);
            Assert.DoesNotContain(".UseDelayedRedelivery(", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TECHNICAL-RETRY-ARCHITECTURE", "public-extensions-forward-the-custom-classifier")]
    public void PublicTechnicalRetryExtensions_ForwardTheCustomClassifierToTheCanonicalPolicies()
    {
        string source = File.ReadAllText(Path.Combine(
            RepositoryLayout.Root,
            "src/ViciOne.ServiceBus/Configuration/TechnicalRetryConfigurationExtensions.cs"));

        Assert.Equal(1, Count(source, "TechnicalRetryPolicy.ConfigureImmediate(retry, classifier)"));
        Assert.Equal(1, Count(source, "TechnicalRetryPolicy.ConfigureRedelivery(redelivery, classifier)"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TECHNICAL-RETRY-ARCHITECTURE", "rabbitmq-native-redelivery-binds-schedule-and-taxonomy")]
    public void RabbitMqNativeRedelivery_BindsTheCanonicalScheduleAndTransientTaxonomy()
    {
        string source = File.ReadAllText(Path.Combine(
            RepositoryLayout.Root,
            "src/Transports/ViciOne.ServiceBus.RabbitMq/Configuration/RabbitMqQueueRedeliveryExtensions.cs"));

        Assert.Equal(1, Count(source, "public static void UseTechnicalQueueRedelivery"));
        Assert.Equal(1, Count(source, "TechnicalRetryPolicy.RedeliveryIntervals.ToArray()"));
        Assert.Equal(1, Count(source, "classifier.Classify(exception) == RetryFailureKind.Transient"));
        Assert.Equal(1, Count(source, "configurator.UseQueueRedelivery(intervals, retry =>"));
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
