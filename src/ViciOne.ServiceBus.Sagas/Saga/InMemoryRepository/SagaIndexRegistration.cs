using System;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Publishes a captured index entry and can remove only its own successful addition.</summary>
internal sealed class SagaIndexRegistration
{
    readonly Func<bool> _apply;
    readonly Action _rollback;
    bool _added;

    public SagaIndexRegistration(object? key, Func<bool> apply, Action rollback)
    {
        Key = key;
        _apply = apply;
        _rollback = rollback;
    }

    public object? Key { get; }

    public void Apply()
    {
        _added = _apply();
    }

    public void Rollback()
    {
        if (!_added)
            return;

        _added = false;
        _rollback();
    }
}
