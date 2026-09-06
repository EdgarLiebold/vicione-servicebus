using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Introspection;

/// <summary>Carries state for scope probe operations.</summary>
public class ScopeProbeContext :
    ProbeContext
{
    readonly CancellationToken _cancellationToken;
    readonly IDictionary<string, object> _variables;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    protected ScopeProbeContext(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _variables = new Dictionary<string, object>();
    }

    CancellationToken ProbeContext.CancellationToken => _cancellationToken;

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    public void Add(string key, string value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (string.IsNullOrEmpty(value))
            _variables.Remove(key);
        else
            _variables[key] = value;
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    public void Add(string key, object value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (value == null || (value is string s && string.IsNullOrEmpty(s)))
            _variables.Remove(key);
        else
            _variables[key] = value;
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="values">The values.</param>
    public void Set(object values)
    {
        if (values != null)
            SetVariablesFromDictionary(ConvertObject.ToDictionary(values));
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="values">The values.</param>
    public void Set(IEnumerable<KeyValuePair<string, object>> values)
    {
        SetVariablesFromDictionary(values);
    }

    /// <summary>Creates scope.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <returns>The created scope.</returns>
    public ProbeContext CreateScope(string key)
    {
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

    /// <summary>Builds the configured component.</summary>
    /// <returns>The configured component.</returns>
    protected IDictionary<string, object> Build()
    {
        return _variables.ToDictionary(x => x.Key, item =>
        {
            if (item.Value is IList<ScopeProbeContext> list)
            {
                if (list.Count == 1)
                    return list[0].Build();

                return list.Select(x => x.Build()).ToArray();
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
