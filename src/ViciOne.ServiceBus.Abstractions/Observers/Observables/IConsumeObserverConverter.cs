using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Calls the generic version of the IPublishEndpoint.Send method with the object's type
/// </summary>
public interface IConsumeObserverConverter
{
    Task PreConsumeAsync(IConsumeObserver observer, object context, CancellationToken cancellationToken = default);

    Task PostConsumeAsync(IConsumeObserver observer, object context, CancellationToken cancellationToken = default);

    Task ConsumeFaultAsync(IConsumeObserver observer, object context, Exception exception, CancellationToken cancellationToken = default);
}
