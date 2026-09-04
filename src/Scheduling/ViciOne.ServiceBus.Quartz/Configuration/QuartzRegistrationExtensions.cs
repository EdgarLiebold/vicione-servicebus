using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Quartz;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>
/// Provides extension methods for quartz registration.
/// </summary>
public static class QuartzRegistrationExtensions
{
    /// <summary>
    /// Add the Quartz consumers to the bus, using <see cref="QuartzEndpointOptions" /> for configuration. Also registers the
    /// Quartz Bus Observer, so that Quartz is started/stopped with the bus.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configure">Configure the Quartz options</param>
    public static void AddQuartzConsumers(this IBusRegistrationConfigurator configurator, Action<QuartzEndpointOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.Services.TryAddSingleton(TimeProvider.System);
        configurator.Services.TryAddTransient<ScheduledMessageJob>();

        OptionsBuilder<QuartzEndpointOptions> options = configurator.Services.AddOptions<QuartzEndpointOptions>();
        if (configure != null)
            options.Configure(configure);

        configurator.Services.AddBusObserver<QuartzBusObserver>();

        configurator.Services.TryAddSingleton<QuartzEndpointDefinition>();

        configurator.AddConsumer<ScheduleMessageConsumer, ScheduleMessageConsumerDefinition>();
        configurator.AddConsumer<CancelScheduledMessageConsumer, CancelScheduledMessageConsumerDefinition>();
        configurator.AddConsumer<PauseScheduledMessageConsumer, PauseScheduledMessageConsumerDefinition>();
        configurator.AddConsumer<ResumeScheduledMessageConsumer, ResumeScheduledMessageConsumerDefinition>();
    }

    /// <summary>
    /// When manually configuring a receive endpoint, configure the Quartz consumers for this endpoint
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="context"></param>
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
