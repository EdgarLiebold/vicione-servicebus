using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides extension methods for new id.</summary>
public static class NewIdExtensions
{
    /// <summary>Converts this value to new id.</summary>
    /// <param name="guid">The guid.</param>
    /// <returns>The converted new id.</returns>
    public static NewId ToNewId(this Guid guid)
    {
        return NewId.FromGuid(guid);
    }

    /// <summary>Converts this value to new id from sequential.</summary>
    /// <param name="guid">The guid.</param>
    /// <returns>The converted new id from sequential.</returns>
    public static NewId ToNewIdFromSequential(this Guid guid)
    {
        return NewId.FromSequentialGuid(guid);
    }
}
