using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Builds a routing slip from its itinerary, variables, subscriptions, logs and exceptions.
/// </summary>
public sealed class RoutingSlipBuilder :
    IRoutingSlipBuilder,
    IRoutingSlipSubscriptionTarget
{
    static readonly IDictionary<string, object> _noArguments =
        new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));

    readonly List<IActivityException> _activityExceptions;
    readonly List<IActivityLog> _activityLogs;
    readonly List<ICompensateLog> _compensateLogs;
    readonly DateTimeOffset _createTimestamp;
    readonly List<IActivity> _itinerary;
    readonly List<IActivity> _sourceItinerary;
    readonly List<ISubscription> _subscriptions;
    readonly Dictionary<string, object> _variables;

    /// <summary>Creates an empty routing-slip builder.</summary>
    /// <param name="trackingNumber">The non-empty identifier of the routing slip.</param>
    /// <param name="timeProvider">The clock used for the creation timestamp.</param>
    public RoutingSlipBuilder(Guid trackingNumber, TimeProvider? timeProvider = null)
    {
        if (trackingNumber == Guid.Empty)
            throw new ArgumentException("The routing-slip tracking number cannot be empty.", nameof(trackingNumber));

        TrackingNumber = trackingNumber;
        _createTimestamp = (timeProvider ?? TimeProvider.System).GetUtcNow();

        _itinerary = new List<IActivity>();
        _sourceItinerary = new List<IActivity>();
        _activityLogs = new List<IActivityLog>();
        _activityExceptions = new List<IActivityException>();
        _compensateLogs = new List<ICompensateLog>();
        _variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        _subscriptions = new List<ISubscription>();
    }

    /// <summary>Creates a builder from a routing slip and a selected itinerary.</summary>
    /// <param name="routingSlip">The routing slip whose state is copied.</param>
    /// <param name="activitySelector">Selects the itinerary copied into the builder.</param>
    internal RoutingSlipBuilder(IRoutingSlip routingSlip, Func<IEnumerable<IActivity>, IEnumerable<IActivity>> activitySelector)
    {
        ArgumentNullException.ThrowIfNull(routingSlip);
        ArgumentNullException.ThrowIfNull(activitySelector);
        IEnumerable<IActivity> selectedItinerary = activitySelector(routingSlip.Itinerary)
            ?? throw new InvalidOperationException("The activity selector returned a null itinerary.");

        TrackingNumber = routingSlip.TrackingNumber;
        _createTimestamp = routingSlip.CreateTimestamp;
        _itinerary = new List<IActivity>(selectedItinerary);
        _activityLogs = new List<IActivityLog>(routingSlip.ActivityLogs);
        _compensateLogs = new List<ICompensateLog>(routingSlip.CompensateLogs);
        _activityExceptions = new List<IActivityException>(routingSlip.ActivityExceptions);
        _variables = new Dictionary<string, object>(routingSlip.Variables, StringComparer.OrdinalIgnoreCase);
        _subscriptions = new List<ISubscription>(routingSlip.Subscriptions);

        _sourceItinerary = new List<IActivity>();
    }

    /// <summary>Creates a builder with explicit active and source itineraries.</summary>
    /// <param name="routingSlip">The routing slip whose remaining state is copied.</param>
    /// <param name="itinerary">The active itinerary.</param>
    /// <param name="sourceItinerary">The source itinerary retained for revision events.</param>
    internal RoutingSlipBuilder(IRoutingSlip routingSlip, IEnumerable<IActivity> itinerary, IEnumerable<IActivity> sourceItinerary)
    {
        ArgumentNullException.ThrowIfNull(routingSlip);
        ArgumentNullException.ThrowIfNull(itinerary);
        ArgumentNullException.ThrowIfNull(sourceItinerary);

        TrackingNumber = routingSlip.TrackingNumber;
        _createTimestamp = routingSlip.CreateTimestamp;
        _itinerary = new List<IActivity>(itinerary);
        _activityLogs = new List<IActivityLog>(routingSlip.ActivityLogs);
        _compensateLogs = new List<ICompensateLog>(routingSlip.CompensateLogs);
        _activityExceptions = new List<IActivityException>(routingSlip.ActivityExceptions);
        _variables = new Dictionary<string, object>(routingSlip.Variables, StringComparer.OrdinalIgnoreCase);
        _subscriptions = new List<ISubscription>(routingSlip.Subscriptions);

        _sourceItinerary = new List<IActivity>(sourceItinerary);
    }

    /// <summary>Creates a builder with an explicit compensation-log sequence.</summary>
    /// <param name="routingSlip">The routing slip whose remaining state is copied.</param>
    /// <param name="compensateLogs">The compensation logs retained by the builder.</param>
    internal RoutingSlipBuilder(IRoutingSlip routingSlip, IEnumerable<ICompensateLog> compensateLogs)
    {
        ArgumentNullException.ThrowIfNull(routingSlip);
        ArgumentNullException.ThrowIfNull(compensateLogs);

        TrackingNumber = routingSlip.TrackingNumber;
        _createTimestamp = routingSlip.CreateTimestamp;
        _itinerary = new List<IActivity>(routingSlip.Itinerary);
        _activityLogs = new List<IActivityLog>(routingSlip.ActivityLogs);
        _compensateLogs = new List<ICompensateLog>(compensateLogs);
        _activityExceptions = new List<IActivityException>(routingSlip.ActivityExceptions);
        _variables = new Dictionary<string, object>(routingSlip.Variables, StringComparer.OrdinalIgnoreCase);
        _subscriptions = new List<ISubscription>(routingSlip.Subscriptions);

        _sourceItinerary = new List<IActivity>();
    }

    /// <summary>Gets the source itinerary retained for revision and termination events.</summary>
    internal IList<IActivity> SourceItinerary => _sourceItinerary.AsReadOnly();

    /// <summary>Gets the routing-slip tracking number.</summary>
    public Guid TrackingNumber { get; }

    /// <summary>Adds an activity to the routing slip without specifying any arguments.</summary>
    /// <param name="name">The activity name.</param>
    /// <param name="executeAddress">The execution address of the activity.</param>
    public void AddActivity(string name, Uri executeAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(executeAddress);

        IActivity activity = new RoutingSlipActivity(name, executeAddress, _noArguments);
        _itinerary.Add(activity);
    }

    /// <summary>Adds an activity to the routing slip specifying activity arguments as an anonymous object.</summary>
    /// <param name="name">The activity name.</param>
    /// <param name="executeAddress">The execution address of the activity.</param>
    /// <param name="arguments">
    /// An object whose non-null, non-default property values override routing-slip variables with matching names.
    /// </param>
    public void AddActivity(string name, Uri executeAddress, object arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(executeAddress);
        ArgumentNullException.ThrowIfNull(arguments);

        IDictionary<string, object> argumentsDictionary = GetActivityArgumentsAsDictionary(arguments);

        IActivity activity = new RoutingSlipActivity(name, executeAddress, argumentsDictionary);
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

        IActivity activity = new RoutingSlipActivity(name, executeAddress, arguments);
        _itinerary.Add(activity);
    }

    /// <summary>Sets a string variable, or removes it when the value is null.</summary>
    /// <param name="key">The variable name.</param>
    /// <param name="value">The variable value, or <see langword="null"/> to remove it.</param>
    public void SetVariable(string key, string? value)
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
    public void SetVariable(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value == null)
            _variables.Remove(key);
        else
            _variables[key] = value;
    }

    /// <summary>Adds, replaces, or removes variables from the readable properties of an object.</summary>
    /// <param name="values">The object whose property names and values define the updates; null values remove variables.</param>
    public void SetVariables(object values)
    {
        ArgumentNullException.ThrowIfNull(values);

        IDictionary<string, object> dictionary = GetObjectAsDictionary(values);

        SetVariablesFromDictionary(dictionary);
    }

    /// <summary>Adds, replaces, or removes variables from a key/value sequence.</summary>
    /// <param name="values">The updates to apply; null values remove variables.</param>
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

    /// <summary>Adds a subscription that receives all optional content for selected lifecycle events.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    public void AddSubscription(Uri address, RoutingSlipEvents events)
    {
        ArgumentNullException.ThrowIfNull(address);
        RoutingSlipSubscriptionSelection.Validate(events, nameof(events));

        _subscriptions.Add(new RoutingSlipSubscription(address, events, RoutingSlipEventContents.All));
    }

    /// <summary>Adds a subscription with an explicit optional-content selection.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    /// <param name="contents">The optional routing-slip data included in delivered events.</param>
    public void AddSubscription(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents)
    {
        ArgumentNullException.ThrowIfNull(address);
        RoutingSlipSubscriptionSelection.Validate(events, nameof(events));
        RoutingSlipSubscriptionSelection.Validate(contents, nameof(contents));

        _subscriptions.Add(new RoutingSlipSubscription(address, events, contents));
    }

    /// <summary>Adds an activity-filtered subscription with an explicit optional-content selection.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    /// <param name="contents">The optional routing-slip data included in delivered events.</param>
    /// <param name="activityName">The non-empty activity name that limits delivery.</param>
    public void AddSubscription(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents, string activityName)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
        RoutingSlipSubscriptionSelection.Validate(events, nameof(events));
        RoutingSlipSubscriptionSelection.Validate(contents, nameof(contents));

        _subscriptions.Add(new RoutingSlipSubscription(address, events, contents, activityName));
    }

    /// <summary>Captures a custom subscription message for selected lifecycle events.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    /// <param name="callback">The asynchronous callback that sends the custom message into the builder.</param>
    /// <param name="cancellationToken">The token that cancels subscription-message creation.</param>
    /// <returns>A task that completes after the custom message has been captured.</returns>
    public Task AddSubscriptionAsync(Uri address, RoutingSlipEvents events, Func<ISendEndpoint, Task> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(callback);
        RoutingSlipSubscriptionSelection.Validate(events, nameof(events));

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return callback(new RoutingSlipSubscriptionCaptureEndpoint(this, address, events, null))
            ?? throw new InvalidOperationException("The subscription callback returned a null task.");
    }

    /// <summary>Captures a custom subscription message with an explicit optional-content selection.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    /// <param name="contents">The optional routing-slip data included in delivered events.</param>
    /// <param name="callback">The asynchronous callback that sends the custom message into the builder.</param>
    /// <param name="cancellationToken">The token that cancels subscription-message creation.</param>
    /// <returns>A task that completes after the custom message has been captured.</returns>
    public Task AddSubscriptionAsync(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents, Func<ISendEndpoint, Task> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(callback);
        RoutingSlipSubscriptionSelection.Validate(events, nameof(events));
        RoutingSlipSubscriptionSelection.Validate(contents, nameof(contents));

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return callback(new RoutingSlipSubscriptionCaptureEndpoint(this, address, events, null, contents))
            ?? throw new InvalidOperationException("The subscription callback returned a null task.");
    }

    /// <summary>Captures an activity-filtered custom subscription message.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    /// <param name="contents">The optional routing-slip data included in delivered events.</param>
    /// <param name="activityName">The non-empty activity name that limits delivery.</param>
    /// <param name="callback">The asynchronous callback that sends the custom message into the builder.</param>
    /// <param name="cancellationToken">The token that cancels subscription-message creation.</param>
    /// <returns>A task that completes after the custom message has been captured.</returns>
    public Task AddSubscriptionAsync(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents, string activityName,
        Func<ISendEndpoint, Task> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
        ArgumentNullException.ThrowIfNull(callback);
        RoutingSlipSubscriptionSelection.Validate(events, nameof(events));
        RoutingSlipSubscriptionSelection.Validate(contents, nameof(contents));

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return callback(new RoutingSlipSubscriptionCaptureEndpoint(this, address, events, activityName, contents))
            ?? throw new InvalidOperationException("The subscription callback returned a null task.");
    }

    /// <summary>Builds the routing slip using the current state of the builder.</summary>
    /// <returns>An immutable snapshot of the routing slip.</returns>
    public IRoutingSlip Build()
    {
        return new RoutingSlipRoutingSlip(TrackingNumber, _createTimestamp, _itinerary, _activityLogs, _compensateLogs, _activityExceptions,
            _variables, _subscriptions);
    }

    /// <summary>Adds a materialized custom message to a validated event subscription.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    /// <param name="contents">The optional routing-slip data included in delivered events.</param>
    /// <param name="activityName">The activity-name filter, when configured.</param>
    /// <param name="message">The serialized custom subscription message.</param>
    void IRoutingSlipSubscriptionTarget.AddSubscription(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents, string? activityName,
        MessageEnvelope message)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(message);
        RoutingSlipSubscriptionSelection.Validate(events, nameof(events));
        RoutingSlipSubscriptionSelection.Validate(contents, nameof(contents));

        _subscriptions.Add(new RoutingSlipSubscription(address, events, contents, activityName, message));
    }

    /// <summary>Adds a completed activity log to the routing slip.</summary>
    /// <param name="host">The host that executed the activity.</param>
    /// <param name="name">The activity name.</param>
    /// <param name="activityTrackingNumber">The non-empty activity execution identifier.</param>
    /// <param name="timestamp">The activity completion timestamp.</param>
    /// <param name="duration">The non-negative activity duration.</param>
    internal void AddActivityLog(HostInfo host, string name, Guid activityTrackingNumber, DateTimeOffset timestamp, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (activityTrackingNumber == Guid.Empty)
            throw new ArgumentException("The activity tracking number cannot be empty.", nameof(activityTrackingNumber));
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);

        _activityLogs.Add(new RoutingSlipActivityLog(host, activityTrackingNumber, name, timestamp, duration));
    }

    /// <summary>Adds the data and destination required to compensate an activity.</summary>
    /// <param name="activityTrackingNumber">The non-empty activity execution identifier.</param>
    /// <param name="compensateAddress">The compensation endpoint address.</param>
    /// <param name="data">The compensation data.</param>
    internal void AddCompensateLog(Guid activityTrackingNumber, Uri compensateAddress, IDictionary<string, object> data)
    {
        if (activityTrackingNumber == Guid.Empty)
            throw new ArgumentException("The activity tracking number cannot be empty.", nameof(activityTrackingNumber));
        ArgumentNullException.ThrowIfNull(compensateAddress);
        ArgumentNullException.ThrowIfNull(data);

        _compensateLogs.Add(new RoutingSlipCompensateLog(activityTrackingNumber, compensateAddress, data));
    }

    /// <summary>Captures a local activity failure in the routing slip.</summary>
    /// <param name="host">The host that executed the activity.</param>
    /// <param name="name">The non-empty failed activity name.</param>
    /// <param name="activityTrackingNumber">The non-empty activity execution identifier.</param>
    /// <param name="timestamp">The failure timestamp.</param>
    /// <param name="elapsed">The non-negative duration before failure.</param>
    /// <param name="exception">The activity failure to capture.</param>
    internal void AddActivityException(HostInfo host, string name, Guid activityTrackingNumber, DateTimeOffset timestamp, TimeSpan elapsed,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (activityTrackingNumber == Guid.Empty)
            throw new ArgumentException("The activity tracking number cannot be empty.", nameof(activityTrackingNumber));
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(exception);

        var exceptionInfo = new FaultExceptionInfo(exception);

        IActivityException activityException = new RoutingSlipActivityException(name, host, activityTrackingNumber, timestamp, elapsed,
            exceptionInfo);
        _activityExceptions.Add(activityException);
    }

    /// <summary>Adds an existing exception snapshot as an activity failure.</summary>
    /// <param name="host">The host that executed the activity.</param>
    /// <param name="name">The non-empty failed activity name.</param>
    /// <param name="activityTrackingNumber">The non-empty activity execution identifier.</param>
    /// <param name="timestamp">The failure timestamp.</param>
    /// <param name="elapsed">The non-negative duration before failure.</param>
    /// <param name="exceptionInfo">The captured activity failure.</param>
    internal void AddActivityException(HostInfo host, string name, Guid activityTrackingNumber, DateTimeOffset timestamp, TimeSpan elapsed,
        ExceptionInfo exceptionInfo)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (activityTrackingNumber == Guid.Empty)
            throw new ArgumentException("The activity tracking number cannot be empty.", nameof(activityTrackingNumber));
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(exceptionInfo);

        IActivityException activityException = new RoutingSlipActivityException(name, host, activityTrackingNumber, timestamp, elapsed, exceptionInfo);
        _activityExceptions.Add(activityException);
    }

    /// <summary>Adds an existing activity-failure record to the routing slip.</summary>
    /// <param name="activityException">The activity-failure record.</param>
    internal void AddActivityException(IActivityException activityException)
    {
        ArgumentNullException.ThrowIfNull(activityException);

        _activityExceptions.Add(activityException);
    }

    void SetVariablesFromDictionary(IEnumerable<KeyValuePair<string, object>> values)
    {
        var validated = new List<KeyValuePair<string, object>>();
        foreach (KeyValuePair<string, object> value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value.Key, nameof(values));
            validated.Add(value);
        }

        foreach (KeyValuePair<string, object> value in validated)
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
    static Dictionary<string, object> GetObjectAsDictionary(object values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return ConvertObject.ToDictionary(values);
    }

    static Dictionary<string, object> GetActivityArgumentsAsDictionary(object arguments)
    {
        Dictionary<string, object> values = GetObjectAsDictionary(arguments);
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach ((string key, object value) in values)
        {
            if (!IsDefaultValue(value))
                result.Add(key, value);
        }

        return result;
    }

    static bool IsDefaultValue(object? value)
    {
        if (value is null)
            return true;

        Type runtimeType = value.GetType();
        return runtimeType.IsValueType && value.Equals(Activator.CreateInstance(runtimeType));
    }
}
