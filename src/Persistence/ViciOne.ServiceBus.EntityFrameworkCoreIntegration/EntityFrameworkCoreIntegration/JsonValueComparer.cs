using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

public class JsonValueComparer<T> :
    ValueComparer<T>
    where T : class?
{
    public JsonValueComparer()
        : base((t1, t2) => DoEquals(t1, t2), t => DoGetHashCode(t), t => DoGetSnapshot(t)!)
    {
    }

    static string? Json(T? instance)
    {
        return instance == null
            ? null
            : JsonSerializer.Serialize(instance, ServiceBusMetadataJson.Options);
    }

    static T? DoGetSnapshot(T? instance)
    {
        if (instance == null)
            return default;

        if (instance is ICloneable cloneable)
            return (T)cloneable.Clone();

        return JsonSerializer.Deserialize<T>(Json(instance)!, ServiceBusMetadataJson.Options);
    }

    static int DoGetHashCode(T? instance)
    {
        if (instance == null)
            return 0;

        if (instance is IEquatable<T>)
            return instance.GetHashCode();

        return Json(instance)?.GetHashCode() ?? 0;
    }

    static bool DoEquals(T? left, T? right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left == null || right == null)
            return false;

        if (left is IEquatable<T> equatable)
            return equatable.Equals(right);

        return string.Equals(Json(left), Json(right), StringComparison.Ordinal);
    }
}
