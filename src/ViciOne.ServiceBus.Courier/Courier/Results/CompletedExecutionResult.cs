using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Courier.Results;

class CompletedExecutionResult<TArguments> :
    BaseExecutionResult<TArguments>,
    CompletedActivityOptions
    where TArguments : class
{
    readonly Uri? _compensationAddress;
    readonly Dictionary<string, object> _data = new(StringComparer.OrdinalIgnoreCase);

    public CompletedExecutionResult(ExecuteContext<TArguments> context, IRoutingSlipEventPublisher publisher, IActivity activity, IRoutingSlip routingSlip,
        Uri? compensationAddress)
        : base(context, publisher, activity, routingSlip)
    {
        _compensationAddress = compensationAddress;
    }

    public void SetLog<TLog>(TLog log)
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(log);

        IEnumerable<KeyValuePair<string, object>> dictionary = Context.SerializerContext.ToDictionary(log);
        SetLog(dictionary);
    }

    public void SetLog(object values)
    {
        ArgumentNullException.ThrowIfNull(values);

        IEnumerable<KeyValuePair<string, object>> objectAsDictionary = Context.SerializerContext.ToDictionary(values);
        SetLog(objectAsDictionary);
    }

    public void SetLog(IEnumerable<KeyValuePair<string, object>> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        foreach (KeyValuePair<string, object> value in values)
            _data[value.Key] = value.Value;
    }

    public override async Task EvaluateAsync(CancellationToken cancellationToken = default)
    {
        var builder = CreateRoutingSlipBuilder(RoutingSlip);

        Build(builder);

        var routingSlip = builder.Build();

        await PublishActivityEventsAsync(routingSlip, builder, cancellationToken).ConfigureAwait(false);

        if (HasNextActivity(routingSlip))
        {
            if (Delay.HasValue)
            {
                void AddForwarderAddress(ConsumeContext consumeContext, SendContext sendContext)
                {
                    var forwarderAddress = consumeContext.ReceiveContext.InputAddress ?? consumeContext.DestinationAddress;
                    if (forwarderAddress != null && forwarderAddress != Context.DestinationAddress)
                        sendContext.Headers.Set(MessageHeaders.ForwarderAddress, forwarderAddress.ToString());
                }

                var executeAddress = routingSlip.GetNextExecuteAddress()
                    ?? throw new RoutingSlipException("The next activity execute address was not specified.");
                await Context.ScheduleSendAsync(executeAddress, Delay.Value, routingSlip, new CopyContextPipe(Context, AddForwarderAddress), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var executeAddress = routingSlip.GetNextExecuteAddress()
                    ?? throw new RoutingSlipException("The next activity execute address was not specified.");
                var endpoint = await Context.GetSendEndpointAsync(executeAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

                await Context.ForwardAsync(endpoint, routingSlip).ConfigureAwait(false);
            }
        }
        else
        {
            var completedTimestamp = Context.Timestamp + Duration;
            var completedDuration = completedTimestamp - RoutingSlip.CreateTimestamp;

            await Publisher.PublishRoutingSlipCompletedAsync(completedTimestamp, completedDuration, routingSlip.Variables, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    protected virtual Task PublishActivityEventsAsync(IRoutingSlip routingSlip, RoutingSlipBuilder builder, CancellationToken cancellationToken)
    {
        return Publisher.PublishRoutingSlipActivityCompletedAsync(Context.ActivityName, Context.ExecutionId, Context.Timestamp, Duration,
            routingSlip.Variables, Activity.Arguments, _data, cancellationToken);
    }

    static bool HasNextActivity(IRoutingSlip routingSlip)
    {
        return routingSlip.Itinerary.Any();
    }

    protected virtual void Build(RoutingSlipBuilder builder)
    {
        builder.AddActivityLog(Context.Host, Activity.Name, Context.ExecutionId, Context.Timestamp, Duration);

        if (Variables.Count > 0)
            builder.SetVariables(Variables);

        if (_compensationAddress != null && _data.Count > 0)
            builder.AddCompensateLog(Context.ExecutionId, _compensationAddress, _data);
    }

    protected virtual RoutingSlipBuilder CreateRoutingSlipBuilder(IRoutingSlip routingSlip)
    {
        return new RoutingSlipBuilder(routingSlip, routingSlip.Itinerary.Skip(1), []);
    }
}
