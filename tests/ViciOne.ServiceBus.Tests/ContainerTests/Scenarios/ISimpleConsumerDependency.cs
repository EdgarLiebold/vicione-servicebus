// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.ContainerTests.Scenarios
{
    using System.Threading.Tasks;


    public interface ISimpleConsumerDependency
    {
        Task<bool> WasDisposed { get; }
        bool SomethingDone { get; }
        void DoSomething();
    }
}
