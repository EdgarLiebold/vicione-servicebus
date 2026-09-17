using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Results;

class CompensatedCompensationResult<TLog> :
    CompensationResult
    where TLog : class
{
    readonly CompensateContext<TLog> _compensateContext;
    readonly ICompensateLog _compensateLog;
    readonly TimeSpan _duration;
    readonly IRoutingSlipEventPublisher _publisher;
    readonly IRoutingSlip _routingSlip;

    public CompensatedCompensationResult(CompensateContext<TLog> compensateContext, IRoutingSlipEventPublisher publisher, ICompensateLog compensateLog,
        IRoutingSlip routingSlip)
    {
        _compensateContext = compensateContext;
        _publisher = publisher;
        _compensateLog = compensateLog;
        _routingSlip = routingSlip;
        _duration = _compensateContext.Elapsed;
    }

    protected Dictionary<string, object> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);
    public async Task EvaluateAsync(CancellationToken cancellationToken = default)
    {
        var builder = CreateRoutingSlipBuilder(_routingSlip);

        Build(builder);

        var routingSlip = builder.Build();

        await _publisher.PublishRoutingSlipActivityCompensatedAsync(_compensateContext.ActivityName, _compensateContext.ExecutionId,
            _compensateContext.Timestamp, _duration, routingSlip.Variables, _compensateLog.Data, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (HasMoreCompensations(routingSlip))
        {
            var compensateAddress = routingSlip.GetNextCompensateAddress()
                ?? throw new RoutingSlipException("The next compensation address was not specified.");
            var endpoint = await _compensateContext.GetSendEndpointAsync(compensateAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            await _compensateContext.ForwardAsync(endpoint, routingSlip).ConfigureAwait(false);
        }
        else
        {
            var faultedTimestamp = _compensateContext.Timestamp + _duration;
            var faultedDuration = faultedTimestamp - _routingSlip.CreateTimestamp;

            await _publisher.PublishRoutingSlipFaultedAsync(faultedTimestamp, faultedDuration, routingSlip.Variables,
                routingSlip.ActivityExceptions.ToArray(), cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    public bool IsFailed([NotNullWhen(true)] out Exception? exception)
    {
        exception = null;
        return false;
    }

    static bool HasMoreCompensations(IRoutingSlip routingSlip)
    {
        return routingSlip.CompensateLogs is { Count: > 0 };
    }

    protected virtual void Build(RoutingSlipBuilder builder)
    {
        if (Variables.Count > 0)
            builder.SetVariables(Variables);
    }

    protected virtual RoutingSlipBuilder CreateRoutingSlipBuilder(IRoutingSlip routingSlip)
    {
        return new RoutingSlipBuilder(routingSlip, SkipLast(routingSlip.CompensateLogs));
    }

    static IEnumerable<T> SkipLast<T>(IEnumerable<T> source)
    {
        using IEnumerator<T> enumerator = source.GetEnumerator();

        if (enumerator.MoveNext())
        {
            var element = enumerator.Current;

            while (enumerator.MoveNext())
            {
                yield return element;
                element = enumerator.Current;
            }
        }
    }

    public void SetVariables(object variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        Dictionary<string, object> dictionary = _compensateContext.SerializerContext.ToDictionary(variables);

        SetVariables(dictionary);
    }

    public void SetVariables(IEnumerable<KeyValuePair<string, object>> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        foreach (KeyValuePair<string, object> value in variables)
            SetVariable(value.Key, value.Value);
    }

    public void SetVariable(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        Variables[key] = value!;
    }
}
