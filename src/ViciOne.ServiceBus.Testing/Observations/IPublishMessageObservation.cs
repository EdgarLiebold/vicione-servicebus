using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Owns a temporary receive endpoint that observes a published message contract.</summary>
/// <typeparam name="TMessage">The published message contract.</typeparam>
public interface IPublishMessageObservation<TMessage> :
    IAsyncDisposable
    where TMessage : class
{
    /// <summary>Gets the first published message context accepted by the observation filter.</summary>
    Task<ConsumeContext<TMessage>> Message { get; }
}
