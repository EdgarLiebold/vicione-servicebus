namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using ViciOne.ServiceBus.Serialization;
    using NUnit.Framework;


    [TestFixture(typeof(SystemTextJsonMessageSerializer))]
    public class Serializing_a_protocol_buffer_message :
        SerializationTest
    {
        [Test]
        public void Should_return_the_array_values()
        {
            var tb = new TradesBookedViciOneServiceBus();
            tb.Trades.Add(new TradeBookedViciOneServiceBus { Currency = "AUD" });
            tb.Trades.Add(new TradeBookedViciOneServiceBus { Currency = "USD" });

            var result = SerializeAndReturn(tb);

            Assert.That(result.Trades, Is.Not.Null);
            Assert.That(result.Trades, Has.Count.EqualTo(2));
        }

        public Serializing_a_protocol_buffer_message(Type serializerType)
            : base(serializerType)
        {
        }
    }
}
