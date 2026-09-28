using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Provides extension methods for sql schedule message.</summary>
public static class SqlScheduleMessageExtensions
{

    /// <summary>Uses the SQL transport's built-in message scheduler.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public static void ConfigureSqlMessageScheduler(this IBusFactoryConfigurator configurator)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var pipeBuilderConfigurator = new SqlMessageSchedulerSpecification();

        configurator.AddPrePipeSpecification(pipeBuilderConfigurator);
    }

    /// <summary>Add a <see cref="IMessageScheduler" /> to the container that uses the SQL Transport message enqueue time to schedule messages.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public static void AddSqlMessageScheduler(this IBusRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.Services.TryAddScoped<IMessageScheduler>(provider =>
        {
            var busInstance = provider.GetRequiredService<Bind<IBus, IBusInstance>>().Value;
            var sendEndpointProvider = provider.GetRequiredService<ISendEndpointProvider>();
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;

            var hostConfiguration = busInstance.HostConfiguration as ISqlHostConfiguration
                ?? throw new ArgumentException("The SQL transport configuration was not found");

            return new MessageScheduler(new SqlScheduleMessageProvider(hostConfiguration, sendEndpointProvider), busInstance.Bus.Topology, timeProvider);
        });
    }

    /// <summary>Add a <see cref="IMessageScheduler" /> to the container that uses the SQL Transport message enqueue time to schedule messages.</summary>
    /// <typeparam name="TBus">The bus type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public static void AddSqlMessageScheduler<TBus>(this IBusRegistrationConfigurator<TBus> configurator)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.Services.TryAddScoped(provider =>
        {
            var busInstance = provider.GetRequiredService<IBusInstance<TBus>>();
            var sendEndpointProvider = provider.GetRequiredService<Bind<TBus, ISendEndpointProvider>>().Value;
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;

            var hostConfiguration = busInstance.HostConfiguration as ISqlHostConfiguration
                ?? throw new ArgumentException("The SQL transport configuration was not found");

            return Bind<TBus>.Create<IMessageScheduler>(
                new MessageScheduler(new SqlScheduleMessageProvider(hostConfiguration, sendEndpointProvider), busInstance.Bus.Topology, timeProvider));
        });
    }
}
