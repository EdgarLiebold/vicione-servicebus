// Package-only Research draft. CTS(TimeSpan,TimeProvider) validates its range before provider dispatch.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using Xunit;

namespace ViciOneReview.CorePackage05;

public static class HostTimeoutAssertions
{
    public static async Task BoundsAsync(string property, string boundary)
    {
        TimeSpan maximum = TimeSpan.FromMilliseconds(uint.MaxValue - 1L);
        TimeSpan? timeout = boundary switch
        {
            "null" => null,
            "maximum" => maximum,
            "fraction" => maximum + TimeSpan.FromTicks(1),
            "too-large" => TimeSpan.FromMilliseconds(uint.MaxValue),
            "maxvalue" => TimeSpan.MaxValue,
            "zero" => TimeSpan.Zero,
            "infinite" => Timeout.InfiniteTimeSpan,
            _ => throw new ArgumentException("Unknown boundary", nameof(boundary))
        };
        bool rejected = boundary is "too-large" or "maxvalue" or "zero" or "infinite";
        var settings = new ViciOneServiceBusHostOptions { WaitUntilStarted = true };
        Assign(settings, property, timeout);
        ValidateOptionsResult named = new ValidateViciOneServiceBusHostOptions().Validate("owned", settings);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<ViciOneServiceBusHostOptions>().Configure(actual => Copy(settings, actual));
        services.AddViciOneServiceBus(bus => { bus.Limits(MessageLimits.Conservative); bus.UsingInMemory(); });
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        IStartupValidator validator = provider.GetRequiredService<IStartupValidator>();
        if (rejected)
        {
            OptionsValidationException failure = Assert.Throws<OptionsValidationException>(validator.Validate);
            AssertDiagnostic(failure.Failures, property, "all");
        }
        else
        {
            validator.Validate();
            ViciOneServiceBusHostOptions actual = provider.GetRequiredService<IOptions<ViciOneServiceBusHostOptions>>().Value;
            Assert.Equal(settings.StartTimeout, actual.StartTimeout);
            Assert.Equal(settings.StopTimeout, actual.StopTimeout);
            Assert.Equal(settings.ConsumerStopTimeout, actual.ConsumerStopTimeout);
            if (timeout.HasValue)
            {
                // Actual supported boundary; dispose immediately, no multiweek wait.
                using var timer = new CancellationTokenSource(timeout.Value, TimeProvider.System);
                Assert.False(timer.IsCancellationRequested);
            }
        }
        // Both the normal startup route and the named public validator are mandatory.
        Assert.Equal(rejected, named.Failed);
        if (rejected) AssertDiagnostic(named.Failures, property, "owned");
    }

    public static Task RelationAsync(string variant)
    {
        var settings = new ViciOneServiceBusHostOptions
        {
            StopTimeout = variant == "unset-stop" ? null : TimeSpan.FromSeconds(1),
            ConsumerStopTimeout = variant == "equal" ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(2)
        };
        ValidateOptionsResult result = new ValidateViciOneServiceBusHostOptions().Validate("owned", settings);
        Assert.Equal(variant == "greater", result.Failed);
        if (variant == "greater")
        {
            string diagnostic = Assert.Single(result.Failures!);
            Assert.Contains("ConsumerStopTimeout", diagnostic, StringComparison.Ordinal);
            Assert.Contains("StopTimeout", diagnostic, StringComparison.Ordinal);
            Assert.Contains("bus 'owned'", diagnostic, StringComparison.Ordinal);
            Assert.Contains("less than or equal", diagnostic, StringComparison.Ordinal);
        }
        return Task.CompletedTask;
    }

    public static async Task HostedRuntimeAsync(string boundary, TimeSpan operationTimeout, CancellationToken caller)
    {
        TimeSpan maximum = TimeSpan.FromMilliseconds(uint.MaxValue - 1L);
        TimeSpan? timeout = boundary switch
        {
            "null" => null,
            "maximum" => maximum,
            "fraction" => maximum + TimeSpan.FromTicks(1),
            _ => throw new ArgumentException("Unknown runtime boundary", nameof(boundary))
        };
        var delivered = new TaskCompletionSource<HostMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        string queue = "owned-host-timeout-" + Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<ViciOneServiceBusHostOptions>().Configure(options =>
        {
            options.WaitUntilStarted = true;
            options.StartTimeout = options.StopTimeout = options.ConsumerStopTimeout = timeout;
        });
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingInMemory((_, transport) => transport.ReceiveEndpoint(queue, endpoint =>
                endpoint.Handler<HostMessage>(context =>
                {
                    delivered.TrySetResult(context.Message);
                    return Task.CompletedTask;
                })));
        });
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        IHostedService[] owned = [];
        await OwnedLifetime.RunAsync(async () =>
        {
            provider.GetRequiredService<IStartupValidator>().Validate();
            owned = provider.GetServices<IHostedService>().ToArray();
            Assert.True(owned.Length >= 2);
            // Public application host lifecycle order; no private hosted-service type lookup.
            foreach (IHostedService service in owned)
                await service.StartAsync(caller).WaitAsync(operationTimeout, caller);
            var bus = provider.GetRequiredService<IBus>();
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri("queue:" + queue), caller);
            Guid identity = Guid.NewGuid();
            await endpoint.SendAsync(new HostMessage(identity), caller).WaitAsync(operationTimeout, caller);
            Assert.Equal(identity, (await delivered.Task.WaitAsync(operationTimeout, caller)).Id);
        }, async () =>
        {
            await OwnedLifetime.RunAsync(() => Task.CompletedTask,
                owned.Reverse().Select<IHostedService, Func<Task>>(service =>
                    () => service.StopAsync(CancellationToken.None).WaitAsync(operationTimeout)).ToArray());
        }, () => provider.DisposeAsync().AsTask().WaitAsync(operationTimeout));
    }

    public static Task CustomProviderCtsAsync(string boundary)
    {
        TimeSpan maximum = TimeSpan.FromMilliseconds(uint.MaxValue - 1L);
        TimeSpan delay = boundary switch
        {
            "maximum" => maximum,
            "fraction" => maximum + TimeSpan.FromTicks(1),
            "too-large" => TimeSpan.FromMilliseconds(uint.MaxValue),
            _ => throw new ArgumentException("Unknown CTS boundary", nameof(boundary))
        };
        var provider = new CountingProvider();
        if (boundary == "too-large")
        {
            ArgumentOutOfRangeException failure = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                using var unexpected = new CancellationTokenSource(delay, provider);
            });
            Assert.Equal("delay", failure.ParamName);
            Assert.Equal(0, provider.Creates);
            Assert.Null(provider.Requested);
        }
        else
        {
            using var timer = new CancellationTokenSource(delay, provider);
            Assert.False(timer.IsCancellationRequested);
            Assert.Equal(1, provider.Creates);
            Assert.Equal<TimeSpan?>(delay, provider.Requested);
        }
        return Task.CompletedTask;
    }

    private sealed class CountingProvider : TimeProvider
    {
        public int Creates;
        public TimeSpan? Requested;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Creates++;
            Requested = dueTime;
            return TimeProvider.System.CreateTimer(callback, state, dueTime, period);
        }
    }

    private static void AssertDiagnostic(IEnumerable<string>? failures, string property, string bus)
    {
        string[] rows = Assert.IsAssignableFrom<IEnumerable<string>>(failures).ToArray();
        Assert.NotEmpty(rows);
        Assert.Contains(rows, row => row.Contains("Host lifecycle", StringComparison.Ordinal)
            && row.Contains("bus '" + bus + "'", StringComparison.Ordinal)
            && row.Contains(property, StringComparison.Ordinal)
            && row.Contains("Set " + property, StringComparison.Ordinal));
    }

    private static void Assign(ViciOneServiceBusHostOptions options, string property, TimeSpan? timeout)
    {
        switch (property)
        {
            case "StartTimeout": options.StartTimeout = timeout; break;
            case "StopTimeout": options.StopTimeout = timeout; break;
            case "ConsumerStopTimeout": options.ConsumerStopTimeout = timeout; break;
            default: throw new ArgumentException("Unknown property", nameof(property));
        }
    }
    private static void Copy(ViciOneServiceBusHostOptions source, ViciOneServiceBusHostOptions target)
    {
        target.WaitUntilStarted = source.WaitUntilStarted;
        target.StartTimeout = source.StartTimeout;
        target.StopTimeout = source.StopTimeout;
        target.ConsumerStopTimeout = source.ConsumerStopTimeout;
    }
    public sealed record HostMessage(Guid Id);
}
