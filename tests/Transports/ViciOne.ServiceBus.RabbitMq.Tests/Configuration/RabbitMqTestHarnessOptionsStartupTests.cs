using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.RabbitMq.Testing;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.Configuration;

public sealed class RabbitMqTestHarnessOptionsStartupTests
{
    [Fact]
    public void HarnessOptions_PreserveEveryConfiguredValue()
    {
        Func<RabbitMQ.Client.IChannel, CancellationToken, Task> configure = (_, _) => Task.CompletedTask;
        using ServiceProvider provider = new ServiceCollection()
            .AddRabbitMqTestHarness(options =>
            {
                options.CreateVirtualHostIfMissing = true;
                options.CleanVirtualHostOnStart = true;
                options.AllowRootVirtualHostCleanup = true;
                options.ConfigureVirtualHostAsync = configure;
            })
            .BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
        RabbitMqTestHarnessOptions options = provider.GetRequiredService<IOptions<RabbitMqTestHarnessOptions>>().Value;
        Assert.True(options.CreateVirtualHostIfMissing);
        Assert.True(options.CleanVirtualHostOnStart);
        Assert.True(options.AllowRootVirtualHostCleanup);
        Assert.Same(configure, options.ConfigureVirtualHostAsync);
    }

    [Fact]
    public void ForceCleaningRootWithoutCleaning_FailsAtStartup()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddRabbitMqTestHarness(options =>
            {
                options.AllowRootVirtualHostCleanup = true;
                options.CleanVirtualHostOnStart = false;
            })
            .BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains("AllowRootVirtualHostCleanup", string.Join(Environment.NewLine, exception.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void CoherentHarnessOptions_PassTheSameStartupBoundary()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddRabbitMqTestHarness(options =>
            {
                options.CleanVirtualHostOnStart = true;
                options.AllowRootVirtualHostCleanup = true;
            })
            .BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
        RabbitMqTestHarnessOptions options = provider.GetRequiredService<IOptions<RabbitMqTestHarnessOptions>>().Value;
        Assert.True(options.CleanVirtualHostOnStart);
        Assert.True(options.AllowRootVirtualHostCleanup);
    }

    [Fact]
    public void Registration_RejectsEveryMissingRequiredInput()
    {
        Assert.Equal("services", Assert.Throws<ArgumentNullException>(() =>
            RabbitMqDependencyInjectionTestingExtensions.AddRabbitMqTestHarness(null!, _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddRabbitMqTestHarness(null!)).ParamName);
    }

    [Fact]
    public void Registration_RejectsBusFirstOrderingBeforeMutatingTheCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBus>(_ => null!);
        int originalCount = services.Count;

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            services.AddRabbitMqTestHarness(_ => { }));

        Assert.Contains("before AddViciOneServiceBus", exception.Message, StringComparison.Ordinal);
        Assert.Equal(originalCount, services.Count);
    }

    [Fact]
    public async Task HostedService_StopHonorsCancellationAsync()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddRabbitMqTestHarness(_ => { })
            .BuildServiceProvider();
        IHostedService hostedService = Assert.Single(provider.GetServices<IHostedService>());
        var cancellationToken = new CancellationToken(canceled: true);

        Task stopTask = hostedService.StopAsync(cancellationToken);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stopTask);
        Assert.True(stopTask.IsCanceled);
    }

    [Fact]
    public async Task HostedService_StartWithNoPreparation_CompletesWithoutBrokerAccessAsync()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddRabbitMqTestHarness(_ => { })
            .BuildServiceProvider();
        IHostedService hostedService = Assert.Single(provider.GetServices<IHostedService>());

        Task startTask = hostedService.StartAsync(TestContext.Current.CancellationToken);
        await startTask;

        Assert.True(startTask.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task HostedService_CreateRootVirtualHostRequest_IsANoOpAsync()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddRabbitMqTestHarness(options => options.CreateVirtualHostIfMissing = true)
            .BuildServiceProvider();
        IHostedService hostedService = Assert.Single(provider.GetServices<IHostedService>());

        Task startTask = hostedService.StartAsync(TestContext.Current.CancellationToken);
        await startTask;

        Assert.True(startTask.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task HostedService_RootCleanupRequiresExplicitOptInBeforeBrokerAccessAsync()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddRabbitMqTestHarness(options => options.CleanVirtualHostOnStart = true)
            .BuildServiceProvider();
        IHostedService hostedService = Assert.Single(provider.GetServices<IHostedService>());

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            hostedService.StartAsync(TestContext.Current.CancellationToken));

        Assert.Contains(nameof(RabbitMqTestHarnessOptions.AllowRootVirtualHostCleanup), exception.Message, StringComparison.Ordinal);
    }
}
