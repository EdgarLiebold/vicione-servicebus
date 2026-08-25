namespace ViciOne.ServiceBus.DependencyInjection.Testing
{
    using System;
    using Configuration;
    using ViciOne.ServiceBus.Testing;
    using ViciOne.ServiceBus.Testing.Implementations;


    public class SagaContainerTestHarnessRegistration<TSaga> :
        ISagaRepositoryDecoratorRegistration<TSaga>
        where TSaga : class, ISaga
    {
        public SagaContainerTestHarnessRegistration(ITestHarness testHarness)
        {
            TestTimeout = testHarness.TestTimeout;
            TimeProvider = testHarness.TimeProvider;

            Consumed = new ReceivedMessageList(testHarness.TestTimeout, testHarness.InactivityToken, TimeProvider);
            Created = new SagaList<TSaga>(testHarness.TestTimeout, testHarness.InactivityToken, TimeProvider);
            Sagas = new SagaList<TSaga>(testHarness.TestTimeout, testHarness.InactivityToken, TimeProvider);
        }

        public TimeSpan TestTimeout { get; }
        public TimeProvider TimeProvider { get; }

        public ReceivedMessageList Consumed { get; }
        public SagaList<TSaga> Created { get; }
        public SagaList<TSaga> Sagas { get; }

        public ISagaRepository<TSaga> DecorateSagaRepository(ISagaRepository<TSaga> repository)
        {
            return new TestSagaRepositoryDecorator<TSaga>(repository, Consumed, Created, Sagas);
        }
    }
}
