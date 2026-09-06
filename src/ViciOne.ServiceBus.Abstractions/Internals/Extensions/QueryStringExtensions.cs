using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Internals;

internal static class QueryStringExtensions
{
    public static bool TryGetValueFromQueryString(this Uri uri, string key, out string? value)
    {
        var found = false;
        value = null;

        foreach (var (candidateKey, candidateValue) in uri.SplitQueryString())
        {
            if (!string.Equals(candidateKey, key, StringComparison.OrdinalIgnoreCase))
                continue;

            if (found)
                throw new InvalidOperationException($"The query string contains the key '{key}' more than once.");

            found = true;
            value = candidateValue ?? string.Empty;
        }

        return found;
    }

    public static T GetValueFromQueryString<T>(this Uri uri, string key, T defaultValue)
        where T : struct
    {
        if (string.IsNullOrEmpty(uri.Query))
            return defaultValue;

        try
        {
            if (!uri.TryGetValueFromQueryString(key, out var value) || string.IsNullOrEmpty(value))
                return defaultValue;

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>Parse the host path, which on a host address might be a virtual host, a scope, etc.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The parsed host path.</returns>
    public static string ParseHostPath(this Uri address)
    {
        var path = address.AbsolutePath;

        if (string.IsNullOrWhiteSpace(path))
            return "/";

        if (path.Length == 1 && path[0] == '/')
            return path;

        var split = path.LastIndexOf('/');
        if (split > 0)
            return Uri.UnescapeDataString(path.Substring(1, split - 1));

        return Uri.UnescapeDataString(path.Substring(1));
    }

    /// <summary>Parse the host path and entity name from the address.</summary>
    /// <param name="address">The address.</param>
    /// <param name="hostPath">Receives the host path produced by the operation.</param>
    /// <param name="entityName">Receives the entity name produced by the operation.</param>
    public static void ParseHostPathAndEntityName(this Uri address, out string hostPath, out string entityName)
    {
        var path = address.AbsolutePath;

        var split = path.LastIndexOf('/');
        if (split > 0)
        {
            hostPath = Uri.UnescapeDataString(path.Substring(1, split - 1));
            entityName = path.Substring(split + 1);
        }
        else
        {
            hostPath = "/";
            entityName = path.Substring(1);
        }

        if (entityName.Contains('%'))
            entityName = Uri.UnescapeDataString(entityName);
    }

    /// <summary>Split the query string into an enumerable stream of tuples.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The enumerable produced by the operation.</returns>
    public static IEnumerable<(string, string?)> SplitQueryString(this Uri address)
    {
        var query = address.Query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(query))
            yield break;

        foreach (var element in query.Split('&'))
        {
            var separator = element.IndexOf('=');
            var key = separator < 0 ? element : element[..separator];
            var value = separator < 0 ? null : element[(separator + 1)..];

            yield return (key.ToLowerInvariant(), value);
        }
    }
}
