using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace BatchClock;

public interface IClockBus : IBus { }
public sealed class RuntimeOwner
{
    public readonly Probe Probe = new();
    public readonly AttachClock Filter;
    public readonly ServiceProvider Provider;
    public readonly IBusControl Bus;
    public ConnectHandle? Observer, Consumer;
    public readonly string Queue = "public-clock-" + Guid.NewGuid().ToString("N");
    bool _started, _stopped, _disposed;
    readonly OwnedLifetime _life;
    public RuntimeOwner(OwnedLifetime life, bool typed, int size, BatchTimeLimitStart start, Func<Item, Clock?> clock, bool forced = false, CancellationToken? token = null)
    {
        _life = life;
        Filter = new AttachClock(clock, token);
        var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton(Probe);
        if (typed)
            services.AddViciOneServiceBus<IClockBus>(c =>
            {
                c.Limits(MessageLimits.Conservative);
                if (!forced) Register(c);
                c.UsingInMemory((ctx, cfg) =>
                {
                    if (forced) cfg.UseFilter<Item>(Filter);
                    else cfg.ReceiveEndpoint(Queue, e => { e.PrefetchCount = 16; e.ConcurrentMessageLimit = 16; e.UseFilter<Item>(Filter); e.ConfigureConsumer<Receiver>(ctx); });
                });
            });
        else
            services.AddViciOneServiceBus(c =>
            {
                c.Limits(MessageLimits.Conservative);
                if (!forced) Register(c);
                c.UsingInMemory((ctx, cfg) =>
                {
                    if (forced) cfg.UseFilter<Item>(Filter);
                    else cfg.ReceiveEndpoint(Queue, e => { e.PrefetchCount = 16; e.ConcurrentMessageLimit = 16; e.UseFilter<Item>(Filter); e.ConfigureConsumer<Receiver>(ctx); });
                });
            });
        void Register(IRegistrationConfigurator c) => c.AddConsumer<Receiver>(consumer => consumer.Options<BatchOptions>(o => o.SetMessageLimit(size).SetConcurrencyLimit(1).SetTimeLimit(TimeSpan.FromSeconds(30)).SetTimeLimitStart(start)));
        Provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        life.Own(Dispose);
        Bus = typed ? Provider.GetRequiredService<IBusInstance<IClockBus>>().BusInstance.BusControl : Provider.GetRequiredService<IBusControl>();
        Observer = Bus.ConnectConsumeObserver(Probe);
        if (forced) Consumer = Bus.ConnectConsumer(() => new Receiver(Probe));
    }
    public Task Start() { _started = true; return Bus.StartAsync(); }
    public async Task Stop() { if (!_started || _stopped) return; await Bus.StopAsync(); _stopped = true; }
    public async Task Send(OwnedLifetime life, Item item, Guid? messageId = null)
    {
        var endpoint = await life.Await(Bus.GetSendEndpointAsync(Consumer != null ? Bus.Address : new Uri("queue:" + Queue)));
        await life.Await(endpoint.SendAsync(item, ctx => ctx.MessageId = messageId ?? item.Identity));
    }
    public async Task Flush() { if (Consumer != null) await Consumer.DisposeAsync(); }
    public async Task Dispose()
    {
        if (_disposed) return;
        List<Exception> failures = [];
        async Task Attempt(Func<Task> operation)
        {
            try { await _life.Track(operation()).WaitAsync(_life.Timeout); } catch (Exception e) { failures.Add(e); }
        }
        await Attempt(Flush); await Attempt(Stop);
        await Attempt(() => Observer?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        await Attempt(() => Provider.DisposeAsync().AsTask());
        try { Filter.Dispose(); } catch (Exception e) { failures.Add(e); }
        _disposed = true;
        if (failures.Count > 0) throw new AggregateException(failures);
    }
}
