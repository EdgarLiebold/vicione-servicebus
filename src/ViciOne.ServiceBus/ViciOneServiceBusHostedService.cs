using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ViciOne.ServiceBus;

public class ViciOneServiceBusHostedService :
    IHostedService,
    IAsyncDisposable
{
    readonly IBusDepot _depot;
    readonly IOptions<ViciOneServiceBusHostOptions> _options;
    Task _startTask = null!;
    bool _stopped;

    public ViciOneServiceBusHostedService(IBusDepot depot, IOptions<ViciOneServiceBusHostOptions> options)
    {
        _depot = depot;
        _options = options;
    }

    public async ValueTask DisposeAsync()
    {
        if (_stopped)
            return;

        if (_options.Value.StopTimeout.HasValue)
        {
            using var tokenSource = new CancellationTokenSource(_options.Value.StopTimeout.Value);

            await _depot.StopAsync(tokenSource.Token).ConfigureAwait(false);
        }
        else
            await _depot.StopAsync(CancellationToken.None).ConfigureAwait(false);

        _stopped = true;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
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

            await (_options.Value.StopTimeout.HasValue
                ? _depot.StopAsync(_options.Value.StopTimeout.Value, cancellationToken)
                : _depot.StopAsync(cancellationToken)).ConfigureAwait(false);
        }
    }
}
