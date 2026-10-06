using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace LoggingLifetime;

public interface IPublicBus : IBus { }
public sealed class PublicBus(IBusControl bus) : BusInstance<IPublicBus>(bus), IPublicBus;
public sealed record PublicMessage(string Marker);
public sealed class Receipt
{
    public readonly TaskCompletionSource<string> Received = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Count;
    public Task Handle(ConsumeContext<PublicMessage> context) { Interlocked.Increment(ref Count); Received.TrySetResult(context.Message.Marker); return Task.CompletedTask; }
}

public sealed class RuntimeOwner
{
    readonly Microsoft.Extensions.Hosting.IHost? _host;
    readonly ServiceProvider? _provider;
    readonly string _kind;
    int _disposed;
    public IServiceProvider Services { get; }
    public readonly RecordingProvider Recorder = new();
    public readonly List<Receipt> Receipts = [];
    public readonly List<IBusControl> Buses = [];
    public ILoggerFactory Factory => Services.GetRequiredService<ILoggerFactory>();
    public bool StartAttempted;
    public bool Stopped;
    public bool Disposed => Volatile.Read(ref _disposed) != 0;
    public CountingFactory? CallbackFactory;

    public RuntimeOwner(OwnedLifetime life, string kind = "default", bool host = false, ILoggerFactory? borrowed = null, bool callback = false, IMeterFactory? faultMeter = null)
    {
        _kind = kind;
        if (host)
        {
            var builder = new HostBuilder().ConfigureServices((_, services) => ConfigureServices(services, borrowed, callback, faultMeter));
            if (kind is "default" or "both") builder.UseViciOneServiceBus((_, c) => ConfigureDefault(c));
            if (kind is "dynamic" or "both") builder.UseViciOneServiceBus<IPublicBus>((_, c) => ConfigureTyped(c));
            if (kind == "explicit") builder.UseViciOneServiceBus<IPublicBus, PublicBus>((_, c) => ConfigureTyped(c));
            _host = builder.Build(); Services = _host.Services;
        }
        else
        {
            var services = new ServiceCollection(); ConfigureServices(services, borrowed, callback, faultMeter);
            services.AddViciOneServiceBus(ConfigureDefault);
            _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }); Services = _provider;
        }
        life.Own(DisposeAsync);
    }
    void ConfigureServices(IServiceCollection services, ILoggerFactory? borrowed, bool callback, IMeterFactory? faultMeter)
    {
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace));
        services.AddSingleton<ILoggerProvider>(_ => Recorder);
        services.AddMetrics();
        services.Configure<ViciOneServiceBusHostOptions>(o => { o.WaitUntilStarted = true; o.StartTimeout = TimeSpan.FromSeconds(8); o.StopTimeout = TimeSpan.FromSeconds(8); });
        if (borrowed != null) services.AddSingleton(borrowed);
        if (callback) services.AddSingleton<ILoggerFactory>(_ => CallbackFactory = new CountingFactory(LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace).AddProvider(Recorder))));
        if (faultMeter != null) services.AddSingleton(faultMeter);
    }
    static MessageLimits Limits() => new() { MaxBodyBytes = 1048576, MaxEnvelopeBytes = 2097152, MaxJsonDepth = 64 };
    void ConfigureDefault(IBusRegistrationConfigurator c)
    {
        var receipt = new Receipt(); Receipts.Add(receipt);
        c.Limits(Limits());
        c.UsingInMemory((_, bus) => bus.ReceiveEndpoint("public-logging-default", endpoint => endpoint.Handler<PublicMessage>(receipt.Handle)));
    }
    void ConfigureTyped(IBusRegistrationConfigurator<IPublicBus> c)
    {
        var receipt = new Receipt(); Receipts.Add(receipt);
        c.Limits(Limits());
        c.UsingInMemory((_, bus) => bus.ReceiveEndpoint("public-logging-typed", endpoint => endpoint.Handler<PublicMessage>(receipt.Handle)));
    }
    public void Resolve()
    {
        if (_host == null || _kind is "default" or "both") Buses.Add(Services.GetRequiredService<IBusControl>());
        if (_host != null && _kind is "dynamic" or "explicit" or "both") Buses.Add(Services.GetRequiredService<IBusInstance<IPublicBus>>().BusInstance.BusControl);
        Assert.Equal(Receipts.Count, Buses.Count);
    }
    public Task Start(CancellationToken token = default)
    {
        StartAttempted = true; Stopped = false;
        return _host != null ? _host.StartAsync(token) : Buses.Single().StartAsync(token);
    }
    public async Task Stop()
    {
        if (!StartAttempted || Stopped || Disposed) return;
        if (_host != null) await _host.StopAsync(); else await Buses.Single().StopAsync();
        Stopped = true;
    }
    public async Task Deliver(OwnedLifetime life, string marker)
    {
        for (int i = 0; i < Buses.Count; i++)
        {
            string expected = marker + "." + i;
            var endpoint = await life.Await(Buses[i].GetSendEndpointAsync(new Uri(i == 0 && _kind is "default" or "both" ? "queue:public-logging-default" : "queue:public-logging-typed")));
            await life.Await(endpoint.SendAsync(new PublicMessage(expected)));
            Assert.Equal(expected, await life.Await(Receipts[i].Received.Task));
        }
        await life.Await(Stop());
        foreach (var receipt in Receipts) Assert.Equal(1, Volatile.Read(ref receipt.Count));
    }
    public async Task DisposeAsync()
    {
        if (Disposed) return;
        List<Exception> failures = [];
        try { await Stop(); } catch (Exception e) { failures.Add(e); }
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            try
            {
                if (_provider != null) await _provider.DisposeAsync();
                else if (_host is IAsyncDisposable asynchronous) await asynchronous.DisposeAsync();
                else _host!.Dispose();
            }
            catch (Exception e) { failures.Add(e); }
        }
        if (failures.Count > 0) throw new AggregateException(failures);
    }
}
