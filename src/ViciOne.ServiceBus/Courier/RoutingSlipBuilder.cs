using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Builds a routing slip from its itinerary, variables, subscriptions, logs and exceptions.
/// </summary>
public sealed class RoutingSlipBuilder :
    IRoutingSlipBuilder,
    IRoutingSlipSendEndpointTarget
{
    static readonly IDictionary<string, object> _noArguments =
        new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));

    readonly List<ActivityException> _activityExceptions;
    readonly List<ActivityLog> _activityLogs;
    readonly List<CompensateLog> _compensateLogs;
    readonly DateTimeOffset _createTimestamp;
    readonly List<Activity> _itinerary;
    readonly List<Activity> _sourceItinerary;
    readonly List<Subscription> _subscriptions;
    readonly IDictionary<string, object> _variables;

    /// <summary>Creates an empty routing-slip builder.</summary>
    /// <param name="trackingNumber">The non-empty identifier of the routing slip.</param>
    /// <param name="timeProvider">The clock used for the creation timestamp.</param>
    public RoutingSlipBuilder(Guid trackingNumber, TimeProvider? timeProvider = null)
    {
        if (trackingNumber == Guid.Empty)
            throw new ArgumentException("The routing-slip tracking number cannot be empty.", nameof(trackingNumber));

        TrackingNumber = trackingNumber;
        _createTimestamp = (timeProvider ?? TimeProvider.System).GetUtcNow();

        _itinerary = new List<Activity>();
        _sourceItinerary = new List<Activity>();
        _activityLogs = new List<ActivityLog>();
        _activityExceptions = new List<ActivityException>();
        _compensateLogs = new List<CompensateLog>();
        _variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        _subscriptions = new List<Subscription>();
    }

    /// <summary>Creates a builder from a routing slip and a selected itinerary.</summary>
    /// <param name="routingSlip">The routing slip whose state is copied.</param>
    /// <param name="activitySelector">Selects the itinerary copied into the builder.</param>
    public RoutingSlipBuilder(RoutingSlip routingSlip, Func<IEnumerable<Activity>, IEnumerable<Activity>> activitySelector)
    {
        ArgumentNullException.ThrowIfNull(routingSlip);
        ArgumentNullException.ThrowIfNull(activitySelector);
        IEnumerable<Activity> selectedItinerary = activitySelector(routingSlip.Itinerary)
            ?? throw new InvalidOperationException("The activity selector returned a null itinerary.");

        TrackingNumber = routingSlip.TrackingNumber;
        _createTimestamp = routingSlip.CreateTimestamp;
        _itinerary = new List<Activity>(selectedItinerary);
        _activityLogs = new List<ActivityLog>(routingSlip.ActivityLogs);
        _compensateLogs = new List<CompensateLog>(routingSlip.CompensateLogs);
        _activityExceptions = new List<ActivityException>(routingSlip.ActivityExceptions);
        _variables = new Dictionary<string, object>(routingSlip.Variables, StringComparer.OrdinalIgnoreCase);
        _subscriptions = new List<Subscription>(routingSlip.Subscriptions);

        _sourceItinerary = new List<Activity>();
    }

    /// <summary>Creates a builder with explicit active and source itineraries.</summary>
    /// <param name="routingSlip">The routing slip whose remaining state is copied.</param>
    /// <param name="itinerary">The active itinerary.</param>
    /// <param name="sourceItinerary">The source itinerary retained for revision events.</param>
    public RoutingSlipBuilder(RoutingSlip routingSlip, IEnumerable<Activity> itinerary, IEnumerable<Activity> sourceItinerary)
    {
        ArgumentNullException.ThrowIfNull(routingSlip);
        ArgumentNullException.ThrowIfNull(itinerary);
        ArgumentNullException.ThrowIfNull(sourceItinerary);

        TrackingNumber = routingSlip.TrackingNumber;
        _createTimestamp = routingSlip.CreateTimestamp;
        _itinerary = new List<Activity>(itinerary);
        _activityLogs = new List<ActivityLog>(routingSlip.ActivityLogs);
        _compensateLogs = new List<CompensateLog>(routingSlip.CompensateLogs);
        _activityExceptions = new List<ActivityException>(routingSlip.ActivityExceptions);
        _variables = new Dictionary<string, object>(routingSlip.Variables, StringComparer.OrdinalIgnoreCase);
        _subscriptions = new List<Subscription>(routingSlip.Subscriptions);

        _sourceItinerary = new List<Activity>(sourceItinerary);
    }

    /// <summary>Creates a builder with an explicit compensation-log sequence.</summary>
    /// <param name="routingSlip">The routing slip whose remaining state is copied.</param>
    /// <param name="compensateLogs">The compensation logs retained by the builder.</param>
    public RoutingSlipBuilder(RoutingSlip routingSlip, IEnumerable<CompensateLog> compensateLogs)
    {
        ArgumentNullException.ThrowIfNull(routingSlip);
        ArgumentNullException.ThrowIfNull(compensateLogs);

        TrackingNumber = routingSlip.TrackingNumber;
        _createTimestamp = routingSlip.CreateTimestamp;
        _itinerary = new List<Activity>(routingSlip.Itinerary);
        _activityLogs = new List<ActivityLog>(routingSlip.ActivityLogs);
        _compensateLogs = new List<CompensateLog>(compensateLogs);
        _activityExceptions = new List<ActivityException>(routingSlip.ActivityExceptions);
        _variables = new Dictionary<string, object>(routingSlip.Variables, StringComparer.OrdinalIgnoreCase);
        _subscriptions = new List<Subscription>(routingSlip.Subscriptions);

        _sourceItinerary = new List<Activity>();
    }

    /// <summary>Gets the source itinerary retained for revision and termination events.</summary>
    public IList<Activity> SourceItinerary => _sourceItinerary.AsReadOnly();

    /// <summary>Gets the routing-slip tracking number.</summary>
    public Guid TrackingNumber { get; }

    /// <summary>Adds an activity to the routing slip without specifying any arguments.</summary>
    /// <param name="name">The activity name.</param>
    /// <param name="executeAddress">The execution address of the activity.</param>
    public void AddActivity(string name, Uri executeAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(executeAddress);

        Activity activity = new RoutingSlipActivity(name, executeAddress, _noArguments);
        _itinerary.Add(activity);
    }

    /// <summary>Adds an activity to the routing slip specifying activity arguments as an anonymous object.</summary>
    /// <param name="name">The activity name.</param>
    /// <param name="executeAddress">The execution address of the activity.</param>
    /// <param name="arguments">An anonymous object of properties matching the argument names of the activity.</param>
    public void AddActivity(string name, Uri executeAddress, object arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(executeAddress);
        ArgumentNullException.ThrowIfNull(arguments);

        IDictionary<string, object> argumentsDictionary = GetObjectAsDictionary(arguments);

        Activity activity = new RoutingSlipActivity(name, executeAddress, argumentsDictionary);
        _itinerary.Add(activity);
    }

    /// <summary>Adds an activity with arguments supplied as a dictionary.</summary>
    /// <param name="name">The activity name.</param>
    /// <param name="executeAddress">The execution address of the activity.</param>
    /// <param name="arguments">A dictionary of name/values matching the activity argument properties.</param>
    public void AddActivity(string name, Uri executeAddress, IDictionary<string, object> arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(executeAddress);
        ArgumentNullException.ThrowIfNull(arguments);

        Activity activity = new RoutingSlipActivity(name, executeAddress, arguments);
        _itinerary.Add(activity);
    }

    /// <summary>Sets a string variable, or removes it when the value is null.</summary>
    /// <param name="key">The variable name.</param>
    /// <param name="value">The variable value, or <see langword="null"/> to remove it.</param>
    public void AddVariable(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value == null)
            _variables.Remove(key);
        else
            _variables[key] = value;
    }

    /// <summary>Sets an object variable, or removes it when the value is null.</summary>
    /// <param name="key">The variable name.</param>
    /// <param name="value">The variable value, or <see langword="null"/> to remove it.</param>
    public void AddVariable(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value == null)
            _variables.Remove(key);
        else
            _variables[key] = value;
    }

    /// <summary>
    /// Sets or adds variables from the public properties of an object.
    /// </summary>
    /// <param name="values">The object whose public properties supply variable names and values.</param>
    public void SetVariables(object values)
    {
        ArgumentNullException.ThrowIfNull(values);

        IDictionary<string, object> dictionary = GetObjectAsDictionary(values);

        SetVariablesFromDictionary(dictionary);
    }

    /// <summary>Sets or adds variables from a key/value sequence.</summary>
    /// <param name="values">The variable names and values.</param>
    public void SetVariables(IEnumerable<KeyValuePair<string, object>> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        SetVariablesFromDictionary(values);
    }

    /// <summary>
    /// Adds the activities from the source itinerary to the new routing slip and removes them from the
    /// source itinerary.
    /// </summary>
    /// <returns>The number of activities moved into the active itinerary.</returns>
    public int AddActivitiesFromSourceItinerary()
    {
        var count = _sourceItinerary.Count;

        foreach (var activity in _sourceItinerary)
            _itinerary.Add(activity);

        _sourceItinerary.Clear();

        return count;
    }

    /// <summary>Add an explicit subscription to the routing slip events.</summary>
    /// <param name="address">The destination address where the events are sent.</param>
    /// <param name="events">The events to include in the subscription.</param>
    public void AddSubscription(Uri address, RoutingSlipEvents events)
    {
        ArgumentNullException.ThrowIfNull(address);

        _subscriptions.Add(new RoutingSlipSubscription(address, events, RoutingSlipEventContents.All));
    }

    /// <summary>Add an explicit subscription to the routing slip events.</summary>
    /// <param name="address">The destination address where the events are sent.</param>
    /// <param name="events">The events to include in the subscription.</param>
    /// <param name="contents">The contents of the routing slip event.</param>
    public void AddSubscription(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents)
    {
        ArgumentNullException.ThrowIfNull(address);

        _subscriptions.Add(new RoutingSlipSubscription(address, events, contents));
    }

    /// <summary>Add an explicit subscription to the routing slip events.</summary>
    /// <param name="address">The destination address where the events are sent.</param>
    /// <param name="events">The events to include in the subscription.</param>
    /// <param name="contents">The contents of the routing slip event.</param>
    /// <param name="activityName">Only send events for the specified activity.</param>
    public void AddSubscription(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents, string activityName)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);

        _subscriptions.Add(new RoutingSlipSubscription(address, events, contents, activityName));
    }

    /// <summary>Adds a message subscription to the routing slip that will be sent at the specified event points.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The routing-slip events that trigger the custom message.</param>
    /// <param name="callback">Configures the custom subscription message.</param>
    /// <param name="cancellationToken">Cancels subscription configuration.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddSubscriptionAsync(Uri address, RoutingSlipEvents events, Func<ISendEndpoint, Task> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(callback);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return callback(new RoutingSlipBuilderSendEndpoint(this, address, events, null))
            ?? throw new InvalidOperationException("The subscription callback returned a null task.");
    }

    /// <summary>Adds a message subscription to the routing slip that will be sent at the specified event points.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The routing-slip events that trigger the custom message.</param>
    /// <param name="contents">The routing-slip content included in the event.</param>
    /// <param name="callback">Configures the custom subscription message.</param>
    /// <param name="cancellationToken">Cancels subscription configuration.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddSubscriptionAsync(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents, Func<ISendEndpoint, Task> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(callback);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return callback(new RoutingSlipBuilderSendEndpoint(this, address, events, null, contents))
            ?? throw new InvalidOperationException("The subscription callback returned a null task.");
    }

    /// <summary>Adds a message subscription to the routing slip that will be sent at the specified event points.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The routing-slip events that trigger the custom message.</param>
    /// <param name="contents">The routing-slip content included in the event.</param>
    /// <param name="activityName">Only send events for the specified activity.</param>
    /// <param name="callback">Configures the custom subscription message.</param>
    /// <param name="cancellationToken">Cancels subscription configuration.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddSubscriptionAsync(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents, string activityName,
        Func<ISendEndpoint, Task> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
        ArgumentNullException.ThrowIfNull(callback);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return callback(new RoutingSlipBuilderSendEndpoint(this, address, events, activityName, contents))
            ?? throw new InvalidOperationException("The subscription callback returned a null task.");
    }

    /// <summary>Builds the routing slip using the current state of the builder.</summary>
    /// <returns>An immutable snapshot of the routing slip.</returns>
    public RoutingSlip Build()
    {
        return new RoutingSlipRoutingSlip(TrackingNumber, _createTimestamp, _itinerary, _activityLogs, _compensateLogs, _activityExceptions,
            _variables, _subscriptions);
    }

    /// <summary>Adds a custom subscription message to the routing slip which is sent at the specified events.</summary>
    /// <param name="address">The destination address where the events are sent.</param>
    /// <param name="events">The events to include in the subscription.</param>
    /// <param name="contents">The contents of the routing slip event.</param>
    /// <param name="activityName">The activity name.</param>
    /// <param name="message">The custom message to be sent.</param>
    void IRoutingSlipSendEndpointTarget.AddSubscription(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents, string? activityName,
        MessageEnvelope message)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(message);

        _subscriptions.Add(new RoutingSlipSubscription(address, events, contents, activityName, message));
    }

    /// <summary>Adds a completed activity log to the routing slip.</summary>
    /// <param name="host">The host that executed the activity.</param>
    /// <param name="name">The activity name.</param>
    /// <param name="activityTrackingNumber">The activity tracking number.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    public void AddActivityLog(HostInfo host, string name, Guid activityTrackingNumber, DateTimeOffset timestamp, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _activityLogs.Add(new RoutingSlipActivityLog(host, activityTrackingNumber, name, timestamp, duration));
    }

    /// <summary>Adds an activity compensation log to the routing slip.</summary>
    /// <param name="activityTrackingNumber">The activity tracking number.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    /// <param name="data">The data.</param>
    public void AddCompensateLog(Guid activityTrackingNumber, Uri compensateAddress, IDictionary<string, object> data)
    {
        ArgumentNullException.ThrowIfNull(compensateAddress);
        ArgumentNullException.ThrowIfNull(data);

        _compensateLogs.Add(new RoutingSlipCompensateLog(activityTrackingNumber, compensateAddress, data));
    }

    /// <summary>Adds an activity exception to the routing slip.</summary>
    /// <param name="host">The host.</param>
    /// <param name="name">The name of the faulted activity.</param>
    /// <param name="activityTrackingNumber">The activity tracking number.</param>
    /// <param name="timestamp">The timestamp of the exception.</param>
    /// <param name="elapsed">The time elapsed from the start of the activity to the exception.</param>
    /// <param name="exception">The exception thrown by the activity.</param>
    public void AddActivityException(HostInfo host, string name, Guid activityTrackingNumber, DateTimeOffset timestamp, TimeSpan elapsed,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(exception);

        var exceptionInfo = new FaultExceptionInfo(exception);

        ActivityException activityException = new RoutingSlipActivityException(name, host, activityTrackingNumber, timestamp, elapsed,
            exceptionInfo);
        _activityExceptions.Add(activityException);
    }

    /// <summary>Adds an activity exception to the routing slip.</summary>
    /// <param name="host">The host.</param>
    /// <param name="name">The name of the faulted activity.</param>
    /// <param name="activityTrackingNumber">The activity tracking number.</param>
    /// <param name="timestamp">The timestamp of the exception.</param>
    /// <param name="elapsed">The time elapsed from the start of the activity to the exception.</param>
    /// <param name="exceptionInfo">The exception info.</param>
    public void AddActivityException(HostInfo host, string name, Guid activityTrackingNumber, DateTimeOffset timestamp, TimeSpan elapsed,
        ExceptionInfo exceptionInfo)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(exceptionInfo);

        ActivityException activityException = new RoutingSlipActivityException(name, host, activityTrackingNumber, timestamp, elapsed, exceptionInfo);
        _activityExceptions.Add(activityException);
    }

    /// <summary>Adds an existing activity exception to the routing slip.</summary>
    /// <param name="activityException">The activity exception.</param>
    public void AddActivityException(ActivityException activityException)
    {
        ArgumentNullException.ThrowIfNull(activityException);

        _activityExceptions.Add(activityException);
    }

    void SetVariablesFromDictionary(IEnumerable<KeyValuePair<string, object>> values)
    {
        foreach (KeyValuePair<string, object> value in values)
        {
            if (value.Value == null)
                _variables.Remove(value.Key);
            else
                _variables[value.Key] = value.Value;
        }
    }

    /// <summary>Maps an object's public properties to a case-insensitive dictionary.</summary>
    /// <param name="values">The object to map.</param>
    /// <returns>The mapped property values.</returns>
    public static IDictionary<string, object> GetObjectAsDictionary(object values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return ConvertObject.ToDictionary(values);
    }
}
