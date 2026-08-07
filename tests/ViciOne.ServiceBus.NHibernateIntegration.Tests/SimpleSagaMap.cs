// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.NHibernateIntegration.Tests
{
    using ViciOne.ServiceBus.Tests.Saga;


    public class SimpleSagaMap :
        SagaClassMapping<SimpleSaga>
    {
        public SimpleSagaMap()
        {
            Property(x => x.Name, x => x.Length(40));
            Property(x => x.Initiated);
            Property(x => x.Observed);
            Property(x => x.Completed);
        }
    }
}
