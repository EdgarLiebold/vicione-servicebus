using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for new id generator.
/// </summary>
public interface INewIdGenerator
{
    /// <summary>
    /// Performs the next operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    NewId Next();

    /// <summary>
    /// Performs the next operation.
    /// </summary>
    /// <param name="ids">The ids value.</param>
    /// <param name="index">The index value.</param>
    /// <param name="count">The count value.</param>
    /// <returns>The result of the operation.</returns>
    ArraySegment<NewId> Next(NewId[] ids, int index, int count);

    /// <summary>
    /// Performs the next guid operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    Guid NextGuid();

    /// <summary>
    /// Performs the next guid operation.
    /// </summary>
    /// <param name="ids">The ids value.</param>
    /// <param name="index">The index value.</param>
    /// <param name="count">The count value.</param>
    /// <returns>The result of the operation.</returns>
    ArraySegment<Guid> NextGuid(Guid[] ids, int index, int count);

    /// <summary>
    /// Performs the next sequential guid operation.
    /// </summary>
    /// <param name="ids">The ids value.</param>
    /// <param name="index">The index value.</param>
    /// <param name="count">The count value.</param>
    /// <returns>The result of the operation.</returns>
    ArraySegment<Guid> NextSequentialGuid(Guid[] ids, int index, int count);

    /// <summary>
    /// Performs the next sequential guid operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    Guid NextSequentialGuid();
}
