using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Quartz;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>Registers Quartz scheduling consumers and their shared receive endpoint.</summary>
public static class QuartzRegistrationExtensions
{
    /// <summary>
    /// Add the Quartz consumers to the bus, using <see cref="QuartzEndpointOptions" /> for configuration. Also registers the
    /// Quartz Bus Observer, so that Quartz is started/stopped with the bus.
    /// </summary>
    /// <param name="configurator">The bus registration to update.</param>
    /// <param name="configure">An optional callback for queue, concurrency, and time-zone resolution.</param>
    public static void AddQuartzConsumers(this IBusRegistrationConfigurator configurator, Action<QuartzEndpointOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.Services.TryAddSingleton(TimeProvider.System);
        configurator.Services.TryAddTransient<ScheduledMessageJob>();

        OptionsBuilder<QuartzEndpointOptions> options = configurator.Services.AddOptions<QuartzEndpointOptions>();
        if (configure != null)
            options.Configure(configure);
        options
            .Validate(
                static value => value.PrefetchCount is null or > 0,
                "Quartz endpoint for bus 'default': PrefetchCount must be greater than zero when specified. Set it to a positive value or leave it unset.")
            .Validate(
                static value => value.ConcurrentMessageLimit is null or > 0,
                "Quartz endpoint for bus 'default': ConcurrentMessageLimit must be greater than zero when specified. Set it to a positive value or leave it unset.")
            .Validate(
                static value => !string.IsNullOrWhiteSpace(value.QueueName),
                "Quartz endpoint for bus 'default': QueueName must not be empty. Set a non-empty queue name.")
            .ValidateOnStart();

        configurator.Services.AddBusObserver<QuartzBusObserver>();

        configurator.Services.TryAddSingleton<QuartzEndpointDefinition>();

        configurator.AddConsumer<ScheduleMessageConsumer, ScheduleMessageConsumerDefinition>();
        configurator.AddConsumer<CancelScheduledMessageConsumer, CancelScheduledMessageConsumerDefinition>();
        configurator.AddConsumer<PauseScheduledMessageConsumer, PauseScheduledMessageConsumerDefinition>();
        configurator.AddConsumer<ResumeScheduledMessageConsumer, ResumeScheduledMessageConsumerDefinition>();
    }

    /// <summary>Adds all four registered Quartz scheduling consumers to a manually configured receive endpoint.</summary>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="context">The bus registration context that resolves the consumers.</param>
    public static void ConfigureQuartzConsumers(this IReceiveEndpointConfigurator configurator, IBusRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        configurator.ConfigureConsumer<ScheduleMessageConsumer>(context);
        configurator.ConfigureConsumer<CancelScheduledMessageConsumer>(context);
        configurator.ConfigureConsumer<PauseScheduledMessageConsumer>(context);
        configurator.ConfigureConsumer<ResumeScheduledMessageConsumer>(context);
    }
}
