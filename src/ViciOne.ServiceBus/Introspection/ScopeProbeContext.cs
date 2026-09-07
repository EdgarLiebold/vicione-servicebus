using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Introspection;

/// <summary>Collects the values and nested scopes emitted by one diagnostic probe node.</summary>
internal class ScopeProbeContext :
    ProbeContext
{
    readonly CancellationToken _cancellationToken;
    readonly IDictionary<string, object> _variables;

    /// <summary>Initializes a probe node that shares the caller's cancellation token.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    protected ScopeProbeContext(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _variables = new Dictionary<string, object>();
    }

    CancellationToken ProbeContext.CancellationToken => _cancellationToken;

    /// <summary>Sets or removes a string value in the current node.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The string to store; an empty value removes the key.</param>
    public void Add(string key, string value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (string.IsNullOrEmpty(value))
            _variables.Remove(key);
        else
            _variables[key] = value;
    }

    /// <summary>Sets or removes a value in the current node.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to store; a null or empty string removes the key.</param>
    public void Add(string key, object value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (value == null || (value is string s && string.IsNullOrEmpty(s)))
            _variables.Remove(key);
        else
            _variables[key] = value;
    }

    /// <summary>Copies readable properties from an object into the current node.</summary>
    /// <param name="values">The values.</param>
    public void Set(object values)
    {
        if (values != null)
            SetVariablesFromDictionary(ConvertObject.ToDictionary(values));
    }

    /// <summary>Copies the supplied values into the current node.</summary>
    /// <param name="values">The values.</param>
    public void Set(IEnumerable<KeyValuePair<string, object>> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        SetVariablesFromDictionary(values);
    }

    /// <summary>Creates and appends a child node under the supplied key.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
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

    void SetVariablesFromDictionary(IEnumerable<KeyValuePair<string, object>> values)
    {
        foreach (KeyValuePair<string, object> value in values)
        {
            if (value.Value == null || (value.Value is string s && string.IsNullOrEmpty(s)))
                _variables.Remove(value.Key);
            else
                _variables[value.Key] = value.Value;
        }
    }
}
