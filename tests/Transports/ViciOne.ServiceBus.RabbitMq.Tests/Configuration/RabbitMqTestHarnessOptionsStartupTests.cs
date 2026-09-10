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
}
