using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Courier.Results;

class FailedCompensationResult<TLog> :
    CompensationResult
    where TLog : class
{
    readonly CompensateContext<TLog> _compensateContext;
    readonly CompensateLog _compensateLog;
    readonly TimeSpan _duration;
    readonly Exception _exception;
    readonly IRoutingSlipEventPublisher _publisher;
    readonly RoutingSlip _routingSlip;

    public FailedCompensationResult(CompensateContext<TLog> compensateContext, IRoutingSlipEventPublisher publisher, CompensateLog compensateLog,
        RoutingSlip routingSlip, Exception exception)
    {
        _compensateContext = compensateContext;
        _publisher = publisher;
        _compensateLog = compensateLog;
        _routingSlip = routingSlip;
        _exception = exception;
        _duration = _compensateContext.Elapsed;
    }

    public Task EvaluateAsync(CancellationToken cancellationToken = default)
    {
        var faultedTimestamp = _compensateContext.Timestamp + _duration;
        var faultedDuration = faultedTimestamp - _routingSlip.CreateTimestamp;

        return _publisher.PublishRoutingSlipActivityCompensationFailedAsync(_compensateContext.ActivityName, _compensateContext.ExecutionId,
            _compensateContext.Timestamp, _duration, faultedTimestamp, faultedDuration, new FaultExceptionInfo(_exception), _routingSlip.Variables,
            _compensateLog.Data, cancellationToken: cancellationToken);
    }

    public bool IsFailed([NotNullWhen(true)] out Exception? exception)
    {
        exception = _exception;
        return true;
    }
}
