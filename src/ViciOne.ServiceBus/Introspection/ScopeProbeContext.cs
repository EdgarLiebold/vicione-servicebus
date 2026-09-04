using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Introspection;

/// <summary>
/// Provides a scope probe context implementation.
/// </summary>
public class ScopeProbeContext :
    ProbeContext
{
    readonly CancellationToken _cancellationToken;
    readonly IDictionary<string, object> _variables;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    protected ScopeProbeContext(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _variables = new Dictionary<string, object>();
    }

    CancellationToken ProbeContext.CancellationToken => _cancellationToken;

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void Add(string key, string value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (string.IsNullOrEmpty(value))
            _variables.Remove(key);
        else
            _variables[key] = value;
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void Add(string key, object value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (value == null || (value is string s && string.IsNullOrEmpty(s)))
            _variables.Remove(key);
        else
            _variables[key] = value;
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="values">The values value.</param>
    public void Set(object values)
    {
        if (values != null)
            SetVariablesFromDictionary(ConvertObject.ToDictionary(values));
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="values">The values value.</param>
    public void Set(IEnumerable<KeyValuePair<string, object>> values)
    {
        SetVariablesFromDictionary(values);
    }

    /// <summary>
    /// Creates scope.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
