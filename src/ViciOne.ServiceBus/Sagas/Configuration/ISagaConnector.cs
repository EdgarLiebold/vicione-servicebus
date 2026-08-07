// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface ISagaConnector
    {
        ISagaSpecification<T> CreateSagaSpecification<T>()
            where T : class, ISaga;

        ConnectHandle ConnectSaga<T>(IConsumePipeConnector consumePipe, ISagaRepository<T> repository, ISagaSpecification<T> specification)
            where T : class, ISaga;
    }
}
