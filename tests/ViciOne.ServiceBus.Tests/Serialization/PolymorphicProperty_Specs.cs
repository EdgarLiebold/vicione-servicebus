namespace ViciOne.ServiceBus.Tests.Serialization
{
    namespace Polymorphic
    {
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using NUnit.Framework;
        using System.Text.Json.Serialization;
        using TestFramework;


        // message interface
        public interface ITestMessage
        {
            TestBaseClass Data { get; }
        }


        // message implementation
        public class TestMessage : ITestMessage
        {
            public TestBaseClass Data { get; set; }
        }


        public interface ITestArrayMessage
        {
            TestBaseClass[] Data { get; }
        }


        public class TestArrayMessage :
            ITestArrayMessage
        {
            public TestBaseClass[] Data { get; set; }
        }


        public interface ITestListMessage
        {
            IList<TestBaseClass> Data { get; }
        }


        public class TestListMessage :
            ITestListMessage
        {
            public IList<TestBaseClass> Data { get; set; }
        }


        /// <summary>
        /// The concrete type has to survive the round trip, and on the kept path that is stated on the
        /// base type rather than on every property. Json.NET carried it per property through
        /// TypeNameHandling, which emitted a $type marker; the product deliberately ignores such a
        /// marker in a body it did not write, so the same assurance is expressed with the declared
        /// polymorphism System.Text.Json offers.
        /// </summary>
        [JsonDerivedType(typeof(TestConcreteClass), "concrete")]
        public abstract class TestBaseClass
        {
        }


        public class TestConcreteClass : TestBaseClass
        {
        }


        [TestFixture]
        public class PolymorphicProperty_Specs :
            InMemoryTestFixture
        {
            [Test]
            public async Task Verify_consumed_message_contains_property()
            {
                ITestMessage message = new TestMessage { Data = new TestConcreteClass() };

                await InputQueueSendEndpoint.Send(message);

                ConsumeContext<ITestMessage> context = await _handled;

                Assert.That(context.Message.Data, Is.InstanceOf<TestConcreteClass>());
            }

            #pragma warning disable NUnit1032
            Task<ConsumeContext<ITestMessage>> _handled;
            #pragma warning restore NUnit1032

            protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
            {
                _handled = Handled<ITestMessage>(configurator);
            }
        }


        [TestFixture]
        public class PolymorphicProperty_Array_Specs :
            InMemoryTestFixture
        {
            [Test]
            public async Task Verify_consumed_message_contains_property()
            {
                ITestArrayMessage message = new TestArrayMessage { Data = new TestBaseClass[] { new TestConcreteClass() } };

                await InputQueueSendEndpoint.Send(message);

                await Task.WhenAny(_handled, _faulted);
                if (_faulted.IsCompleted)
                    Assert.Fail("Should not faulted");

                ConsumeContext<ITestArrayMessage> context = await _handled;

                Assert.That(context.Message.Data, Is.Not.Null);
                Assert.That(context.Message.Data, Has.Length.EqualTo(1));

                Assert.That(context.Message.Data[0], Is.InstanceOf<TestConcreteClass>());
            }

            #pragma warning disable NUnit1032
            Task<ConsumeContext<ITestArrayMessage>> _handled;
            Task<ConsumeContext<ReceiveFault>> _faulted;
            #pragma warning restore NUnit1032

            protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
            {
                _handled = Handled<ITestArrayMessage>(configurator);
                _faulted = Handled<ReceiveFault>(configurator);
            }
        }


        [TestFixture]
        public class PolymorphicProperty_List_Specs :
            InMemoryTestFixture
        {
            [Test]
            public async Task Verify_consumed_message_contains_property()
            {
                ITestListMessage message = new TestListMessage { Data = new List<TestBaseClass> { new TestConcreteClass() } };

                await InputQueueSendEndpoint.Send(message);

                await Task.WhenAny(_handled, _faulted);
                if (_faulted.IsCompleted)
                    Assert.Fail("Should not faulted");

                ConsumeContext<ITestListMessage> context = await _handled;

                Assert.That(context.Message.Data, Is.Not.Null);
                Assert.That(context.Message.Data, Has.Count.EqualTo(1));

                Assert.That(context.Message.Data[0], Is.InstanceOf<TestConcreteClass>());
            }

            #pragma warning disable NUnit1032
            Task<ConsumeContext<ITestListMessage>> _handled;
            Task<ConsumeContext<ReceiveFault>> _faulted;
            #pragma warning restore NUnit1032

            protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
            {
                _handled = Handled<ITestListMessage>(configurator);
                _faulted = Handled<ReceiveFault>(configurator);
            }
        }
    }
}
