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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="resource">The resource.</param>
    public ConditionExpression(ISignalResource resource)
    {
        _resource = resource ?? throw new ArgumentNullException(nameof(resource));
    }

    /// <summary>Reevaluates state after a condition changes.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConditionUpdatedAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (TryCheckCondition(out var isMet) && isMet)
            _resource.Signal();
        return Task.CompletedTask;
    }

    /// <summary>Adds a condition block where all conditions in the array must be logically ANDed together to succeed.</summary>
    /// <param name="conditions">The conditions.</param>
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

    /// <summary>Clears all conditions.</summary>
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

    /// <summary>Checks condition.</summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool CheckCondition()
    {
        if (!TryCheckCondition(out var isMet))
            throw new InvalidOperationException("Cannot check an empty condition.");

        return isMet;
    }

    /// <summary>Releases the resources owned by this instance.</summary>
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
