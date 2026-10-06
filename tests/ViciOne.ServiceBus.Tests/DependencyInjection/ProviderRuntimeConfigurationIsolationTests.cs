using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ProviderRuntimeConfigurationIsolationTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [RequirementCoverage("REQ-VSB-DI-DEFINITION-OWNERSHIP", "runtime-consumer-callbacks-stay-with-explicit-endpoint")]
    public async Task ConsumerRuntimeCallback_DoesNotCrossProviderOrEndpointAsync(bool shared, bool disposeFirst)
    {
        ServiceCollection services = ConfigureConsumer();
        var options = new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true };
        await using ServiceProvider first = services.BuildServiceProvider(options);
        await using ServiceProvider second = (shared ? services : ConfigureConsumer()).BuildServiceProvider(options);
        Marker firstMarker = first.GetRequiredService<Marker>();
        firstMarker.Manual = true;
        _ = first.GetRequiredService<IBusInstance<ITestBus>>();
        Assert.Equal(1, firstMarker.GlobalCalls);
        Assert.Equal(1, firstMarker.LocalCalls);
        if (disposeFirst)
            await first.DisposeAsync();

        Marker secondMarker = second.GetRequiredService<Marker>();
        Assert.NotSame(firstMarker, secondMarker);
        _ = second.GetRequiredService<IBusInstance<ITestBus>>();
        Assert.Equal(1, secondMarker.GlobalCalls);
        Assert.Equal(0, secondMarker.LocalCalls);
        Assert.Equal(1, firstMarker.GlobalCalls);
        Assert.Equal(1, firstMarker.LocalCalls);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, false, false)]
    [RequirementCoverage("REQ-VSB-DI-DEFINITION-OWNERSHIP", "manual-activity-does-not-change-other-provider-exclusion-policy")]
    public async Task ManualActivityEndpoint_DoesNotChangeOtherProviderExclusionPolicyAsync(bool shared, bool disposeFirst,
        bool excludeGlobally)
    {
        ServiceCollection services = ConfigureActivity(excludeGlobally);
        var options = new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true };
        await using ServiceProvider first = services.BuildServiceProvider(options);
        await using ServiceProvider second = (shared ? services : ConfigureActivity(excludeGlobally)).BuildServiceProvider(options);
        Marker firstMarker = first.GetRequiredService<Marker>();
        firstMarker.Manual = true;
        _ = first.GetRequiredService<IBusInstance<ITestBus>>();
        Assert.Equal(1, firstMarker.ExecuteCalls);
        Assert.Equal(0, firstMarker.CompensateCalls);
        if (disposeFirst)
            await first.DisposeAsync();

        Marker secondMarker = second.GetRequiredService<Marker>();
        Assert.NotSame(firstMarker, secondMarker);
        _ = second.GetRequiredService<IBusInstance<ITestBus>>();
        Assert.Equal(excludeGlobally ? 0 : 1, secondMarker.ExecuteCalls);
        Assert.Equal(excludeGlobally ? 0 : 1, secondMarker.CompensateCalls);
        Assert.Equal(1, firstMarker.ExecuteCalls);
        Assert.Equal(0, firstMarker.CompensateCalls);
    }

    static ServiceCollection ConfigureConsumer()
    {
        ServiceCollection services = CreateServices();
        services.AddViciOneServiceBus<ITestBus>(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.AddConsumer<TestConsumer>((context, _) => context.GetRequiredService<Marker>().GlobalCalls++);
            registration.UsingInMemory(new Uri("loopback://localhost/provider-runtime-consumer/"), (context, bus) =>
            {
                Marker marker = context.GetRequiredService<Marker>();
                bus.ReceiveEndpoint("consumer", endpoint => context.ConfigureConsumer<TestConsumer>(endpoint,
                    marker.Manual ? _ => marker.LocalCalls++ : null));
                bus.ConfigureEndpoints(context);
            });
        });
        return services;
    }

    static ServiceCollection ConfigureActivity(bool excludeGlobally)
    {
        ServiceCollection services = CreateServices();
        services.AddViciOneServiceBus<ITestBus>(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            IActivityRegistrationConfigurator<TestActivity, Arguments, Log> activity =
                registration.AddActivity<TestActivity, Arguments, Log, RecordingActivityDefinition>();
            if (excludeGlobally)
                activity.ExcludeFromConfigureEndpoints();
            registration.UsingInMemory(new Uri("loopback://localhost/provider-runtime-activity/"), (context, bus) =>
            {
                if (context.GetRequiredService<Marker>().Manual)
                    bus.ReceiveEndpoint("activity-execute", endpoint => context.ConfigureActivityExecute(typeof(TestActivity),
                        endpoint, new Uri("loopback://localhost/provider-runtime-activity/activity-compensate")));
                bus.ConfigureEndpoints(context);
            });
        });
        return services;
    }

    static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Marker>(_ => new Marker());
        return services;
    }

    public interface ITestBus : IBus;
    public sealed record Message;
    public sealed record Arguments;
    public sealed record Log;
    public sealed class Marker
    {
        public bool Manual { get; set; }
        public int GlobalCalls { get; set; }
        public int LocalCalls { get; set; }
        public int ExecuteCalls { get; set; }
        public int CompensateCalls { get; set; }
    }
    public sealed class TestConsumer : IConsumer<Message>
    {
        public Task ConsumeAsync(ConsumeContext<Message> context) => Task.CompletedTask;
    }
    public sealed class TestActivity : IActivity<Arguments, Log>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Arguments> context) => throw new NotSupportedException();
        public Task<CompensationResult> CompensateAsync(CompensateContext<Log> context) => throw new NotSupportedException();
    }
    public sealed class RecordingActivityDefinition : ActivityDefinition<TestActivity, Arguments, Log>
    {
        protected override void ConfigureExecuteActivity(IReceiveEndpointConfigurator endpoint,
            IExecuteActivityConfigurator<TestActivity, Arguments> activity, IRegistrationContext context)
            => context.GetRequiredService<Marker>().ExecuteCalls++;
        protected override void ConfigureCompensateActivity(IReceiveEndpointConfigurator endpoint,
            ICompensateActivityConfigurator<TestActivity, Log> activity, IRegistrationContext context)
            => context.GetRequiredService<Marker>().CompensateCalls++;
    }
}
