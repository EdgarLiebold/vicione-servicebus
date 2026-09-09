using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Builds the activities, variables, and event subscriptions of a routing-slip itinerary.</summary>
public interface IItineraryBuilder
{
    /// <summary>Gets the routing-slip tracking number.</summary>
    Guid TrackingNumber { get; }

    /// <summary>Adds an activity to the routing slip without specifying any arguments.</summary>
    /// <param name="name">The activity name.</param>
    /// <param name="executeAddress">The execution address of the activity.</param>
    void AddActivity(string name, Uri executeAddress);

    /// <summary>Adds an activity whose arguments are provided by an object projection.</summary>
    /// <param name="name">The activity name.</param>
    /// <param name="executeAddress">The execution address of the activity.</param>
    /// <param name="arguments">An anonymous object of properties matching the argument names of the activity.</param>
    void AddActivity(string name, Uri executeAddress, object arguments);

    /// <summary>Adds an activity whose arguments are provided by a name/value dictionary.</summary>
    /// <param name="name">The activity name.</param>
    /// <param name="executeAddress">The execution address of the activity.</param>
    /// <param name="arguments">A dictionary of name/values matching the activity argument properties.</param>
    void AddActivity(string name, Uri executeAddress, IDictionary<string, object> arguments);

    /// <summary>Adds, updates, or removes a string variable.</summary>
    /// <param name="key">The variable name.</param>
    /// <param name="value">The new value, or <see langword="null"/> to remove the variable.</param>
    void SetVariable(string key, string? value);

    /// <summary>Adds, updates, or removes an object variable.</summary>
    /// <param name="key">The variable name.</param>
    /// <param name="value">The new value, or <see langword="null"/> to remove the variable.</param>
    void SetVariable(string key, object? value);

    /// <summary>
    /// Adds or replaces routing-slip variables from the readable properties of an object.
    /// A property whose value is <see langword="null" /> removes the matching variable.
    /// </summary>
    /// <param name="values">The object whose readable properties supply variable names and values.</param>
    void SetVariables(object values);

    /// <summary>Adds, updates, or removes multiple routing-slip variables.</summary>
    /// <param name="values">The variables; an entry with a null value removes the matching variable.</param>
    void SetVariables(IEnumerable<KeyValuePair<string, object>> values);

    /// <summary>Appends every activity retained from the source itinerary.</summary>
    /// <returns>The number of activities added to the itinerary.</returns>
    int AddActivitiesFromSourceItinerary();

    /// <summary>Adds a subscription that receives all optional content for selected lifecycle events.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    void AddSubscription(Uri address, RoutingSlipEvents events);

    /// <summary>Adds a subscription with an explicit optional-content selection.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    /// <param name="contents">The optional routing-slip data included in delivered events.</param>
    void AddSubscription(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents);

    /// <summary>Captures custom subscription messages for selected lifecycle events.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    /// <param name="callback">The asynchronous callback that sends custom messages into the builder.</param>
    /// <param name="cancellationToken">The token that cancels subscription-message creation.</param>
    /// <returns>A task that completes after the callback and its message captures complete.</returns>
    Task AddSubscriptionAsync(Uri address, RoutingSlipEvents events, Func<ISendEndpoint, Task> callback, CancellationToken cancellationToken = default);

    /// <summary>Captures custom subscription messages with an explicit optional-content selection.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    /// <param name="contents">The optional routing-slip data included in delivered events.</param>
    /// <param name="callback">The asynchronous callback that sends custom messages into the builder.</param>
    /// <param name="cancellationToken">The token that cancels subscription-message creation.</param>
    /// <returns>A task that completes after the callback and its message captures complete.</returns>
    Task AddSubscriptionAsync(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents, Func<ISendEndpoint, Task> callback, CancellationToken cancellationToken = default);

    /// <summary>Adds an activity-filtered subscription with an explicit optional-content selection.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    /// <param name="contents">The optional routing-slip data included in delivered events.</param>
    /// <param name="activityName">The non-empty activity name that limits delivery.</param>
    void AddSubscription(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents, string activityName);

    /// <summary>Captures activity-filtered custom subscription messages.</summary>
    /// <param name="address">The subscription destination.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    /// <param name="contents">The optional routing-slip data included in delivered events.</param>
    /// <param name="activityName">The non-empty activity name that limits delivery.</param>
    /// <param name="callback">The asynchronous callback that sends custom messages into the builder.</param>
    /// <param name="cancellationToken">The token that cancels subscription-message creation.</param>
    /// <returns>A task that completes after the callback and its message captures complete.</returns>
    Task AddSubscriptionAsync(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents, string activityName, Func<ISendEndpoint, Task> callback, CancellationToken cancellationToken = default);
}
