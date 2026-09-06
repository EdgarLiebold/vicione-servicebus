using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Carries state for future.</summary>
public class FutureState :
    SagaStateMachineInstance,
    IConsumerKindOwnedState,
    ISagaVersion
{
    Dictionary<Guid, FutureMessage>? _faults = [];
    HashSet<Guid>? _pending = [];
    Dictionary<Guid, FutureMessage>? _results = [];
    HashSet<FutureSubscription>? _subscriptions = new(FutureSubscription.Comparer);
    Dictionary<string, object>? _variables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets the current state.</summary>
    public int CurrentState { get; set; }

    /// <summary>Gets or sets the created.</summary>
    public DateTimeOffset Created { get; set; }
    /// <summary>Gets or sets the completed.</summary>
    public DateTimeOffset? Completed { get; set; }
    /// <summary>Gets or sets the faulted.</summary>
    public DateTimeOffset? Faulted { get; set; }

    /// <summary>Gets or sets the location.</summary>
    public Uri Location { get; set; } = null!;

    /// <summary>Gets or sets the command.</summary>
    public FutureMessage Command { get; set; } = null!;

    /// <summary>Gets or sets the pending.</summary>
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

    /// <summary>Gets or sets the subscriptions.</summary>
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

    /// <summary>Gets or sets the variables.</summary>
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

    /// <summary>Gets or sets the results.</summary>
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

    /// <summary>Gets or sets the faults.</summary>
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

    /// <summary>Gets or sets the row version.</summary>
    public byte[] RowVersion { get; set; } = null!;
    /// <summary>Gets or sets the version.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets the correlation id.</summary>
    public Guid CorrelationId { get; set; }

    /// <summary>Determines whether the current value has subscriptions.</summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasSubscriptions()
    {
        return _subscriptions != null && _subscriptions.Count > 0;
    }

    /// <summary>Determines whether the current value has variables.</summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasVariables()
    {
        return _variables != null && _variables.Count > 0;
    }

    /// <summary>Determines whether the current value has results.</summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasResults()
    {
        return _results != null && _results.Count > 0;
    }

    /// <summary>Determines whether the current value has faults.</summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasFaults()
    {
        return _faults != null && _faults.Count > 0;
    }

    /// <summary>Determines whether the current value has pending.</summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasPending()
    {
        return _pending != null && _pending.Count > 0;
    }
}
