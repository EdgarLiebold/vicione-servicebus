using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// A collection of blocks of conditions that must occur to signal a resource.
/// Each condition block in the list is logically OR'd with the other condition blocks.
/// Each condition within a condition block is logically AND'd with the other conditions in the same block.
/// </summary>
public class ConditionExpression :
    IDisposable,
    IConditionObserver
{
    readonly List<IObservableCondition[]> _conditionBlocks = new List<IObservableCondition[]>();
    readonly List<ConnectHandle> _connections = new List<ConnectHandle>();
    readonly object _lock = new object();
    readonly ISignalResource _resource;
    bool _disposed;

    public ConditionExpression(ISignalResource resource)
    {
        _resource = resource ?? throw new ArgumentNullException(nameof(resource));
    }

    public Task ConditionUpdatedAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (TryCheckCondition(out var isMet) && isMet)
            _resource.Signal();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds a condition block where all conditions in the array must be logically ANDed together to succeed.
    /// </summary>
    public void AddConditionBlock(params IObservableCondition[] conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        if (conditions.Length == 0)
            throw new ArgumentException("Must add at least 1 condition to a condition block.");
        if (conditions.Any(condition => condition == null))
            throw new ArgumentException("A condition block cannot contain a null condition.", nameof(conditions));

        var connections = new List<ConnectHandle>(conditions.Length);
        try
        {
            foreach (var condition in conditions)
                connections.Add(condition.ConnectConditionObserver(this));

            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _conditionBlocks.Add(conditions.ToArray());
                _connections.AddRange(connections);
            }
        }
        catch
        {
            connections.ForEach(connection => connection.Disconnect());
            throw;
        }
    }

    public void ClearAllConditions()
    {
        ConnectHandle[] connections;
        lock (_lock)
        {
            connections = _connections.ToArray();
            _connections.Clear();
            _conditionBlocks.Clear();
        }

        foreach (var connection in connections)
            connection.Disconnect();
    }

    public bool CheckCondition()
    {
        if (!TryCheckCondition(out var isMet))
            throw new InvalidOperationException("Cannot check an empty condition.");

        return isMet;
    }

    public void Dispose()
    {
        ConnectHandle[] connections;
        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;
            connections = _connections.ToArray();
            _connections.Clear();
            _conditionBlocks.Clear();
        }

        foreach (var connection in connections)
            connection.Disconnect();
    }

    bool TryCheckCondition(out bool isMet)
    {
        IObservableCondition[][] conditionBlocks;
        lock (_lock)
            conditionBlocks = _conditionBlocks.Select(block => block.ToArray()).ToArray();

        if (conditionBlocks.Length == 0)
        {
            isMet = false;
            return false;
        }

        isMet = conditionBlocks.Any(conditionBlock => conditionBlock.All(condition => condition.IsMet));
        return true;
    }
}
