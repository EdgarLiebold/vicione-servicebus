using System;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Publishes one captured index entry once and can roll back only its own successful addition.</summary>
/// <remarks>Concurrent or reentrant lifecycle transitions are rejected. A failed rollback remains retryable.</remarks>
internal sealed class SagaIndexRegistration
{
    readonly Func<bool> _apply;
    readonly object _lock = new();
    readonly Action _rollback;
    RegistrationState _state;

    /// <summary>Creates an unapplied registration with required apply and rollback callbacks.</summary>
    /// <param name="key">The captured key, including null when supported by the index.</param>
    /// <param name="apply">The callback that publishes the entry and reports whether it owns an addition.</param>
    /// <param name="rollback">The callback that removes an owned addition.</param>
    public SagaIndexRegistration(object? key, Func<bool> apply, Action rollback)
    {
        Key = key;
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        _rollback = rollback ?? throw new ArgumentNullException(nameof(rollback));
    }

    /// <summary>Gets the exact key captured before publication.</summary>
    public object? Key { get; }

    /// <summary>Attempts publication exactly once.</summary>
    public void Apply()
    {
        lock (_lock)
        {
            if (_state != RegistrationState.Captured)
                throw new InvalidOperationException("The saga index registration has already attempted publication.");

            _state = RegistrationState.Applying;
        }

        try
        {
            bool added = _apply();
            lock (_lock)
                _state = added ? RegistrationState.Applied : RegistrationState.NotAdded;
        }
        catch
        {
            lock (_lock)
                _state = RegistrationState.ApplyFailed;
            throw;
        }
    }

    /// <summary>Removes a successfully published entry once; a failed removal may be retried.</summary>
    public void Rollback()
    {
        lock (_lock)
        {
            if (_state is RegistrationState.Captured or RegistrationState.NotAdded or RegistrationState.ApplyFailed or RegistrationState.RolledBack)
                return;
            if (_state != RegistrationState.Applied)
                throw new InvalidOperationException("The saga index registration has a lifecycle transition in progress.");

            _state = RegistrationState.RollingBack;
        }

        try
        {
            _rollback();
            lock (_lock)
                _state = RegistrationState.RolledBack;
        }
        catch
        {
            lock (_lock)
                _state = RegistrationState.Applied;
            throw;
        }
    }

    enum RegistrationState
    {
        Captured,
        Applying,
        Applied,
        NotAdded,
        ApplyFailed,
        RollingBack,
        RolledBack,
    }
}
