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
    readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    readonly object _stateLock = new();
    readonly IOptions<ViciOneServiceBusHostOptions> _options;
    readonly IServiceProvider _provider;
    IBusDepot? _depot;
    Task? _startTask;
    bool _stopping;
    bool _stopped;

    public ViciOneServiceBusHostedService(IServiceProvider provider, IOptions<ViciOneServiceBusHostOptions> options)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async ValueTask DisposeAsync()
    {
        await _lifecycleGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            IBusDepot? depot;
            lock (_stateLock)
            {
                if (_stopped)
                    return;

                _stopping = true;
                depot = _depot;
            }

            if (depot is not null)
            {
                if (_options.Value.StopTimeout is { } stopTimeout)
                {
                    using var tokenSource = new CancellationTokenSource(stopTimeout);
                    await depot.StopAsync(tokenSource.Token).ConfigureAwait(false);
                }
                else
                    await depot.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }

            lock (_stateLock)
                _stopped = true;
        }
        finally
        {
            lock (_stateLock)
                _stopping = false;

            _lifecycleGate.Release();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_stateLock)
        {
            if (_stopped || _stopping)
                throw new InvalidOperationException("The hosted service cannot be started after stopping has begun.");

            if (_startTask is null)
            {
                _depot = _provider.GetRequiredService<IBusDepot>();
                _startTask = _options.Value.StartTimeout.HasValue
                    ? _depot.StartAsync(_options.Value.StartTimeout.Value, cancellationToken)
                    : _depot.StartAsync(cancellationToken);
            }

            return _startTask.IsCompleted || _options.Value.WaitUntilStarted
                ? _startTask
                : Task.CompletedTask;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_stateLock)
        {
            if (_stopped)
                return;
        }

        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IBusDepot? depot;
            lock (_stateLock)
            {
                if (_stopped)
                    return;

                _stopping = true;
                depot = _depot;
            }

            if (depot is not null)
                await (_options.Value.StopTimeout.HasValue
                    ? depot.StopAsync(_options.Value.StopTimeout.Value, cancellationToken)
                    : depot.StopAsync(cancellationToken)).ConfigureAwait(false);

            lock (_stateLock)
                _stopped = true;
        }
        finally
        {
            lock (_stateLock)
                _stopping = false;

            _lifecycleGate.Release();
        }
    }
}
