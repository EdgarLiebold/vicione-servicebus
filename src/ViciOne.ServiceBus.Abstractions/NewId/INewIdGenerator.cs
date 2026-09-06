using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Defines the operations required by new id generator.</summary>
public interface INewIdGenerator
{
    /// <summary>Advances to the next value.</summary>
    /// <returns>The new id produced by the operation.</returns>
    NewId Next();

    /// <summary>Advances to the next value.</summary>
    /// <param name="ids">The ids.</param>
    /// <param name="index">The index.</param>
    /// <param name="count">The count.</param>
    /// <returns>The array segment produced by the operation.</returns>
    ArraySegment<NewId> Next(NewId[] ids, int index, int count);

    /// <summary>Generates the next ordered identifier.</summary>
    /// <returns>The guid produced by the operation.</returns>
    Guid NextGuid();

    /// <summary>Generates the next ordered identifier.</summary>
    /// <param name="ids">The ids.</param>
    /// <param name="index">The index.</param>
    /// <param name="count">The count.</param>
    /// <returns>The array segment produced by the operation.</returns>
    ArraySegment<Guid> NextGuid(Guid[] ids, int index, int count);

    /// <summary>Generates the next sequential identifier.</summary>
    /// <param name="ids">The ids.</param>
    /// <param name="index">The index.</param>
    /// <param name="count">The count.</param>
    /// <returns>The array segment produced by the operation.</returns>
    ArraySegment<Guid> NextSequentialGuid(Guid[] ids, int index, int count);

    /// <summary>Generates the next sequential identifier.</summary>
    /// <returns>The guid produced by the operation.</returns>
    Guid NextSequentialGuid();
}
