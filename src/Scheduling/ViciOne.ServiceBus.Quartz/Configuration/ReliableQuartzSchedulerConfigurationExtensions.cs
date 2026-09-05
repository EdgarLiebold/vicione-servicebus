using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Quartz;
using ViciOne.ServiceBus.Scheduling;

#nullable enable

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Selects Quartz-backed scheduling adapters inside a reliable-messaging block.
/// </summary>
public static class ReliableQuartzSchedulerConfigurationExtensions
{
    /// <summary>
    /// Selects a caller-registered Quartz scheduler and its durable scheduling endpoint.
    /// </summary>
    /// <param name="configurator">The owning reliable-messaging configurator.</param>
    /// <param name="configure">Optional endpoint configuration.</param>
    /// <returns>The same configurator.</returns>
    /// <remarks>
    /// The application must register exactly one <see cref="ISchedulerFactory" />. No in-memory
    /// scheduler is substituted when that dependency is absent.
    /// </remarks>
    public static IReliableMessagingConfigurator UseQuartzScheduler(
        this IReliableMessagingConfigurator configurator,
        Action<QuartzEndpointOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        IReliableMessagingProviderConfigurator provider = RequireProvider(configurator);
        ConfigureQuartzAdapter(configurator, provider, configure);
        return configurator;
    }

    /// <summary>
    /// Selects an explicitly volatile in-memory Quartz scheduler for tests and local development.
    /// </summary>
    /// <param name="configurator">The owning reliable-messaging configurator.</param>
    /// <param name="configure">Optional endpoint configuration.</param>
    /// <returns>The same configurator.</returns>
    public static IReliableMessagingConfigurator UseInMemoryScheduler(
        this IReliableMessagingConfigurator configurator,
        Action<QuartzEndpointOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        IReliableMessagingProviderConfigurator provider = RequireProvider(configurator);
        if (provider.Services.Any(static descriptor => descriptor.ServiceType == typeof(ISchedulerFactory)))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Scheduling",
                    "unknown",
                    "An ISchedulerFactory is already registered; the in-memory scheduler would create a second timing owner.",
                    "Choose either UseQuartzScheduler or UseInMemoryScheduler exactly once"));
        }

        provider.Services.AddSingleton(QuartzIntegrationExtensions.CreateSchedulerFactory());
        ConfigureQuartzAdapter(configurator, provider, configure);
        return configurator;
    }

    static void ConfigureQuartzAdapter(
        IReliableMessagingConfigurator configurator,
        IReliableMessagingProviderConfigurator provider,
        Action<QuartzEndpointOptions>? configure)
    {
        var options = new QuartzEndpointOptions();
        configure?.Invoke(options);
        if (string.IsNullOrWhiteSpace(options.QueueName))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Scheduling",
                    "unknown",
                    "The Quartz scheduler queue name must not be empty.",
                    "Set a non-empty QueueName"));
        }

        provider.RegistrationConfigurator.AddQuartzConsumers(target => Copy(options, target));
        RegisterSchedulerContracts(configurator);
        provider.UseEndpointSchedulerAdapter(new Uri($"queue:{options.QueueName}", UriKind.Absolute));
    }

    static IReliableMessagingProviderConfigurator RequireProvider(IReliableMessagingConfigurator configurator)
    {
        if (configurator is IReliableMessagingProviderConfigurator provider)
            return provider;

        throw new ConfigurationException(
            global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                "Scheduling",
                "unknown",
                "The reliable-messaging configurator does not expose its Quartz adapter contract.",
                "Choose the adapter inside UseReliableMessaging"));
    }

    static void RegisterSchedulerContracts(IReliableMessagingConfigurator configurator)
    {
        configurator.AddMessageContract<ScheduleMessage>("vicione.scheduler.schedule", 1);
        configurator.AddMessageContract<CancelScheduledMessage>("vicione.scheduler.cancel", 1);
        configurator.AddMessageContract<ScheduleRecurringMessage>("vicione.scheduler.recurring.schedule", 1);
        configurator.AddMessageContract<PauseScheduledRecurringMessage>("vicione.scheduler.recurring.pause", 1);
        configurator.AddMessageContract<ResumeScheduledRecurringMessage>("vicione.scheduler.recurring.resume", 1);
    }

    static void Copy(QuartzEndpointOptions source, QuartzEndpointOptions target)
    {
        target.PrefetchCount = source.PrefetchCount;
        target.ConcurrentMessageLimit = source.ConcurrentMessageLimit;
        target.QueueName = source.QueueName;
        target.TimeZoneResolver = source.TimeZoneResolver;
    }
}
