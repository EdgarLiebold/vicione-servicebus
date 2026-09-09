using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Operations;

/// <summary>Collects the values and nested scopes emitted by one diagnostic probe node.</summary>
internal class ScopeProbeContext :
    ProbeContext
{
    readonly CancellationToken _cancellationToken;
    readonly IDictionary<string, object> _variables;

    /// <summary>Initializes a probe node that shares the caller's cancellation token.</summary>
    /// <param name="cancellationToken">The token that cancels snapshot collection.</param>
    protected ScopeProbeContext(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _variables = new Dictionary<string, object>();
    }

    CancellationToken ProbeContext.CancellationToken => _cancellationToken;

    /// <summary>Sets or removes a string value in the current node.</summary>
    /// <param name="key">The non-empty diagnostic key.</param>
    /// <param name="value">The string to store; null or empty removes the key.</param>
    public void Add(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (string.IsNullOrEmpty(value))
            _variables.Remove(key);
        else
            _variables[key] = value;
    }

    /// <summary>Sets or removes a value in the current node.</summary>
    /// <param name="key">The non-empty diagnostic key.</param>
    /// <param name="value">The value to store; a null or empty string removes the key.</param>
    public void Add(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value == null || (value is string s && string.IsNullOrEmpty(s)))
            _variables.Remove(key);
        else
            _variables[key] = value;
    }

    /// <summary>Copies readable properties from an object into the current node.</summary>
    /// <param name="values">The object whose readable properties provide diagnostic values.</param>
    public void Set(object values)
    {
        ArgumentNullException.ThrowIfNull(values);
        SetVariablesFromDictionary(ConvertObject.ToDictionary(values));
    }

    /// <summary>Copies the supplied values into the current node.</summary>
    /// <param name="values">The diagnostic values to copy.</param>
    public void Set(IEnumerable<KeyValuePair<string, object?>> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        SetVariablesFromDictionary(values);
    }

    /// <summary>Creates and appends a child node under the supplied key.</summary>
    /// <param name="key">The non-empty key that groups scopes of the same kind.</param>
    /// <returns>The created scope.</returns>
    public ProbeContext CreateScope(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var scope = new ScopeProbeContext(_cancellationToken);

        IList<ScopeProbeContext> list;

        if (_variables.TryGetValue(key, out var value))
        {
            if (value is not IList<ScopeProbeContext> existingScopes)
                throw new InvalidOperationException("The key already exists and is not a scope collection: " + key);

            list = existingScopes;
        }
        else
        {
            list = new List<ScopeProbeContext>();
            _variables[key] = list;
        }

        list.Add(scope);
        return scope;
    }

    /// <summary>Builds a structurally read-only snapshot of this node and every child node.</summary>
    /// <returns>The read-only diagnostic node.</returns>
    protected IReadOnlyDictionary<string, object> BuildResults()
    {
        return _variables.ToFrozenDictionary(x => x.Key, item =>
        {
            if (item.Value is IList<ScopeProbeContext> list)
            {
                if (list.Count == 1)
                    return list[0].BuildResults();

                return list.Select(x => x.BuildResults()).ToList().AsReadOnly();
            }

            return item.Value;
        });
    }

    void SetVariablesFromDictionary<TValue>(IEnumerable<KeyValuePair<string, TValue>> values)
    {
        foreach (KeyValuePair<string, TValue> value in values)
        {
            if (string.IsNullOrWhiteSpace(value.Key))
                throw new ArgumentException("A diagnostic key cannot be null, empty, or whitespace.", nameof(values));

            object? itemValue = value.Value;

            if (itemValue == null || (itemValue is string s && string.IsNullOrEmpty(s)))
                _variables.Remove(value.Key);
            else
                _variables[value.Key] = itemValue;
        }
    }
}
