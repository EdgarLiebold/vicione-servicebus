// ViciOne modification: WP-F2-SERVICEBUS-A-PLUS-RECOVERY-03, 2026-08-16.
namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests
{
    using System;
    using System.Text.Json;
    using AzureServiceBusTransport;
    using NUnit.Framework;


    /// <summary>
    /// A probe tells an operator which store holds the saga state, so it has to name the store this repository
    /// actually uses. Every other repository names its own; this one reported a foreign product it never used.
    /// </summary>
    [TestFixture]
    public class MessageSessionSagaProbe_Specs
    {
        [Test]
        public void Should_name_the_store_it_actually_uses()
        {
            var factory = new MessageSessionSagaRepositoryContextFactory<ProbedSaga>(null);

            var probe = factory.GetProbeResult();

            var persistence = JsonSerializer.SerializeToElement(probe.Results).GetProperty("persistence").GetString();

            Assert.That(persistence, Is.EqualTo("azure-service-bus-message-session"),
                "The message session repository keeps saga state in the session state of the broker and must say so");
        }


        class ProbedSaga :
            ISaga
        {
            public Guid CorrelationId { get; set; }
        }
    }
}
