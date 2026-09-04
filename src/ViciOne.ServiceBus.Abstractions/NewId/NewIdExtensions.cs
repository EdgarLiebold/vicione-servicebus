using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides extension methods for new id.
/// </summary>
public static class NewIdExtensions
{
    /// <summary>
    /// Performs the to new id operation.
    /// </summary>
    /// <param name="guid">The guid value.</param>
    /// <returns>The result of the operation.</returns>
    public static NewId ToNewId(this Guid guid)
    {
        return NewId.FromGuid(guid);
    }

    /// <summary>
    /// Performs the to new id from sequential operation.
    /// </summary>
    /// <param name="guid">The guid value.</param>
    /// <returns>The result of the operation.</returns>
    public static NewId ToNewIdFromSequential(this Guid guid)
    {
        return NewId.FromSequentialGuid(guid);
    }
}
