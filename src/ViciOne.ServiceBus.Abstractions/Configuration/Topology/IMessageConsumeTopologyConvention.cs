using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides optional consume topology for one message contract.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public interface IMessageConsumeTopologyConvention<TMessage> :
    IMessageConsumeTopologyConvention
    where TMessage : class
{
    /// <summary>Attempts to resolve topology applicable to the message contract.</summary>
    /// <param name="messageConsumeTopology">The applicable topology when available.</param>
    /// <returns><see langword="true" /> when topology is applicable; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageConsumeTopology([NotNullWhen(true)] out IMessageConsumeTopology<TMessage>? messageConsumeTopology);
}


/// <summary>Projects an untyped consume convention to compatible message contracts.</summary>
public interface IMessageConsumeTopologyConvention
{
    /// <summary>Attempts to project this convention to a message contract.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="convention">The typed convention when compatible.</param>
    /// <returns><see langword="true" /> when the convention supports <typeparamref name="T" />; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageConsumeTopologyConvention<T>([NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
        where T : class;
}
