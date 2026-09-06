using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Represents the durable lifecycle, messages, variables, and subscribers of one future instance.</summary>
public sealed class FutureState :
    SagaStateMachineInstance,
    IConsumerKindOwnedState,
    ISagaVersion
{
    readonly object _syncRoot = new();
    Dictionary<Guid, FutureMessage>? _faults = [];
    HashSet<Guid>? _pending = [];
    Dictionary<Guid, FutureMessage>? _results = [];
    HashSet<FutureSubscription>? _subscriptions = new(FutureSubscription.Comparer);
    Dictionary<string, object>? _variables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets the state-machine state identifier.</summary>
    public int CurrentState { get; set; }

    /// <summary>Gets or sets when the future was created.</summary>
    public DateTimeOffset Created { get; set; }
    /// <summary>Gets or sets when the future completed successfully.</summary>
    public DateTimeOffset? Completed { get; set; }
    /// <summary>Gets or sets when the future faulted.</summary>
    public DateTimeOffset? Faulted { get; set; }

    /// <summary>Gets or sets the endpoint address that owns the future instance.</summary>
    public Uri Location { get; set; } = null!;

    /// <summary>Gets or sets the command that created the future.</summary>
    public FutureMessage Command { get; set; } = null!;

    /// <summary>Gets or sets the identifiers of operations that must finish before the future can terminate.</summary>
    public HashSet<Guid> Pending
    {
        get
        {
            if (_pending != null)
                return _pending;

            lock (_syncRoot)
                _pending ??= new HashSet<Guid>();

            return _pending;
        }
        set => _pending = value;
    }

    /// <summary>Gets or sets the endpoints subscribed to the future result.</summary>
    public HashSet<FutureSubscription> Subscriptions
    {
        get
        {
            if (_subscriptions != null)
                return _subscriptions;

            lock (_syncRoot)
                _subscriptions ??= new HashSet<FutureSubscription>(FutureSubscription.Comparer);

            return _subscriptions;
        }
        set => _subscriptions = value;
    }

    /// <summary>Gets or sets named values shared by future activities.</summary>
    public Dictionary<string, object> Variables
    {
        get
        {
            if (_variables != null)
                return _variables;

            lock (_syncRoot)
                _variables ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            return _variables;
        }
        set => _variables = value != null ? new Dictionary<string, object>(value, StringComparer.OrdinalIgnoreCase) : null;
    }

    /// <summary>Gets or sets successful operation results by operation identifier.</summary>
    public Dictionary<Guid, FutureMessage> Results
    {
        get
        {
            if (_results != null)
                return _results;

            lock (_syncRoot)
                _results ??= new Dictionary<Guid, FutureMessage>();

            return _results;
        }
        set => _results = value;
    }

    /// <summary>Gets or sets operation faults by operation identifier.</summary>
    public Dictionary<Guid, FutureMessage> Faults
    {
        get
        {
            if (_faults != null)
                return _faults;

            lock (_syncRoot)
                _faults ??= new Dictionary<Guid, FutureMessage>();

            return _faults;
        }
        set => _faults = value;
    }

    /// <summary>Gets or sets the provider-specific optimistic-concurrency token.</summary>
    public byte[] RowVersion { get; set; } = [];
    /// <summary>Gets or sets the numeric optimistic-concurrency version.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets the identifier of the future instance.</summary>
    public Guid CorrelationId { get; set; }

    /// <summary>Determines whether at least one endpoint is subscribed to the result.</summary>
    /// <returns><see langword="true" /> when subscriptions exist; otherwise, <see langword="false" />.</returns>
    public bool HasSubscriptions()
    {
        return _subscriptions != null && _subscriptions.Count > 0;
    }

    /// <summary>Determines whether the future contains named variables.</summary>
    /// <returns><see langword="true" /> when variables exist; otherwise, <see langword="false" />.</returns>
    public bool HasVariables()
    {
        return _variables != null && _variables.Count > 0;
    }

    /// <summary>Determines whether at least one operation completed successfully.</summary>
    /// <returns><see langword="true" /> when results exist; otherwise, <see langword="false" />.</returns>
    public bool HasResults()
    {
        return _results != null && _results.Count > 0;
    }

    /// <summary>Determines whether at least one operation faulted.</summary>
    /// <returns><see langword="true" /> when faults exist; otherwise, <see langword="false" />.</returns>
    public bool HasFaults()
    {
        return _faults != null && _faults.Count > 0;
    }

    /// <summary>Determines whether the future is waiting for an operation.</summary>
    /// <returns><see langword="true" /> when pending operations exist; otherwise, <see langword="false" />.</returns>
    public bool HasPending()
    {
        return _pending != null && _pending.Count > 0;
    }
}
