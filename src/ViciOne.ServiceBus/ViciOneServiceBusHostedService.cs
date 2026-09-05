using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus;

internal sealed class ViciOneServiceBusHostedService :
    IHostedService,
    IAsyncDisposable
{
    readonly IServiceProvider _provider;
    readonly IOptions<ViciOneServiceBusHostOptions> _options;
    IBusDepot? _depot;
    Task _startTask = null!;
    bool _stopped;

    public ViciOneServiceBusHostedService(IServiceProvider provider, IOptions<ViciOneServiceBusHostOptions> options)
    {
        _provider = provider;
        _options = options;
    }

    public async ValueTask DisposeAsync()
    {
        if (_stopped)
            return;

        if (_options.Value.StopTimeout.HasValue)
        {
            using var tokenSource = new CancellationTokenSource(_options.Value.StopTimeout.Value);

            if (_depot is not null)
                await _depot.StopAsync(tokenSource.Token).ConfigureAwait(false);
        }
        else if (_depot is not null)
            await _depot.StopAsync(CancellationToken.None).ConfigureAwait(false);

        _stopped = true;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _depot = _provider.GetRequiredService<IBusDepot>();
        _startTask = _options.Value.StartTimeout.HasValue
            ? _depot.StartAsync(_options.Value.StartTimeout.Value, cancellationToken)
            : _depot.StartAsync(cancellationToken);

        return _startTask.IsCompleted || _options.Value.WaitUntilStarted
            ? _startTask
            : Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_stopped)
        {
            _stopped = true;

            if (_depot is not null)
                await (_options.Value.StopTimeout.HasValue
                ? _depot.StopAsync(_options.Value.StopTimeout.Value, cancellationToken)
                : _depot.StopAsync(cancellationToken)).ConfigureAwait(false);
        }
    }
}
