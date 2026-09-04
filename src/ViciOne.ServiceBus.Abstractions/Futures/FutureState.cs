using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// Provides a future state implementation.
/// </summary>
public class FutureState :
    SagaStateMachineInstance,
    ISagaVersion
{
    Dictionary<Guid, FutureMessage>? _faults;
    HashSet<Guid>? _pending;
    Dictionary<Guid, FutureMessage>? _results;
    HashSet<FutureSubscription>? _subscriptions;
    Dictionary<string, object>? _variables;

    /// <summary>
    /// Gets or sets the current state value.
    /// </summary>
    public int CurrentState { get; set; }

    /// <summary>
    /// Gets or sets the created value.
    /// </summary>
    public DateTimeOffset Created { get; set; }
    /// <summary>
    /// Gets or sets the completed value.
    /// </summary>
    public DateTimeOffset? Completed { get; set; }
    /// <summary>
    /// Gets or sets the faulted value.
    /// </summary>
    public DateTimeOffset? Faulted { get; set; }

    /// <summary>
    /// Gets or sets the location value.
    /// </summary>
    public Uri Location { get; set; } = null!;

    /// <summary>
    /// Gets or sets the command value.
    /// </summary>
    public FutureMessage Command { get; set; } = null!;

    /// <summary>
    /// Gets or sets the pending value.
    /// </summary>
    public HashSet<Guid> Pending
    {
        get
        {
            if (_pending != null)
                return _pending;

            lock (this)
                _pending ??= new HashSet<Guid>();

            return _pending;
        }
        set => _pending = value;
    }

    /// <summary>
    /// Gets or sets the subscriptions value.
    /// </summary>
    public HashSet<FutureSubscription> Subscriptions
    {
        get
        {
            if (_subscriptions != null)
                return _subscriptions;

            lock (this)
                _subscriptions ??= new HashSet<FutureSubscription>(FutureSubscription.Comparer);

            return _subscriptions;
        }
        set => _subscriptions = value;
    }

    /// <summary>
    /// Gets or sets the variables value.
    /// </summary>
    public Dictionary<string, object> Variables
    {
        get
        {
            if (_variables != null)
                return _variables;

            lock (this)
                _variables ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            return _variables;
        }
        set => _variables = value != null ? new Dictionary<string, object>(value, StringComparer.OrdinalIgnoreCase) : null;
    }

    /// <summary>
    /// Gets or sets the results value.
    /// </summary>
    public Dictionary<Guid, FutureMessage> Results
    {
        get
        {
            if (_results != null)
                return _results;

            lock (this)
                _results ??= new Dictionary<Guid, FutureMessage>();

            return _results;
        }
        set => _results = value;
    }

    /// <summary>
    /// Gets or sets the faults value.
    /// </summary>
    public Dictionary<Guid, FutureMessage> Faults
    {
        get
        {
            if (_faults != null)
                return _faults;

            lock (this)
                _faults ??= new Dictionary<Guid, FutureMessage>();

            return _faults;
        }
        set => _faults = value;
    }

    /// <summary>
    /// Gets or sets the row version value.
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;
    /// <summary>
    /// Gets or sets the version value.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the correlation id value.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Determines whether the current value has subscriptions.
    /// </summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasSubscriptions()
    {
        return _subscriptions != null && _subscriptions.Count > 0;
    }

    /// <summary>
    /// Determines whether the current value has variables.
    /// </summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasVariables()
    {
        return _variables != null && _variables.Count > 0;
    }

    /// <summary>
    /// Determines whether the current value has results.
    /// </summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasResults()
    {
        return _results != null && _results.Count > 0;
    }

    /// <summary>
    /// Determines whether the current value has faults.
    /// </summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasFaults()
    {
        return _faults != null && _faults.Count > 0;
    }

    /// <summary>
    /// Determines whether the current value has pending.
    /// </summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasPending()
    {
        return _pending != null && _pending.Count > 0;
    }
}
