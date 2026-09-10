using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.AzureServiceBus.Testing;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests.Configuration;

public sealed class AzureServiceBusTestHarnessOptionsStartupTests
{
    [Fact]
    public void HarnessOptions_AreBoundToTheStartupValidator()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddAzureServiceBusTestHarness(options => options.CleanNamespaceOnStart = true)
            .BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
        Assert.True(provider.GetRequiredService<IOptions<AzureServiceBusTestHarnessOptions>>().Value.CleanNamespaceOnStart);
    }

    [Fact]
    public void Registration_RejectsEveryMissingRequiredInput()
    {
        Assert.Equal("services", Assert.Throws<ArgumentNullException>(() =>
            AzureServiceBusDependencyInjectionTestingExtensions.AddAzureServiceBusTestHarness(null!, _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddAzureServiceBusTestHarness(null!)).ParamName);
    }

    [Fact]
    public void Registration_RejectsBusFirstOrderingBeforeMutatingTheCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBus>(_ => null!);
        int originalCount = services.Count;

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            services.AddAzureServiceBusTestHarness(_ => { }));

        Assert.Contains("before AddViciOneServiceBus", exception.Message, StringComparison.Ordinal);
        Assert.Equal(originalCount, services.Count);
    }

    [Fact]
    public async Task HostedService_StopHonorsCancellationAsync()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddAzureServiceBusTestHarness(_ => { })
            .BuildServiceProvider();
        IHostedService hostedService = Assert.Single(provider.GetServices<IHostedService>());
        var cancellationToken = new CancellationToken(canceled: true);

        Task stopTask = hostedService.StopAsync(cancellationToken);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stopTask);
        Assert.True(stopTask.IsCanceled);
    }
}
