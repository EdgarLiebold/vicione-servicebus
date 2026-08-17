namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;


    [TestFixture]
    [Category("Flaky")]
    public class When_using_a_name_property_for_correlation :
        RabbitMqTestFixture
    {
        [Test]
        public async Task Should_handle_named_property()
        {
            var transactionId = NewId.NextGuid();

            await InputQueueSendEndpoint.Send<OtherMessage>(new { CorrelationId = transactionId });

            ConsumeContext<OtherMessage> otherContext = await _otherHandled;

            Assert.Multiple(() =>
            {
                Assert.That(otherContext.CorrelationId.HasValue, Is.True);
                Assert.That(otherContext.CorrelationId.Value, Is.EqualTo(transactionId));
            });
        }

        Task<ConsumeContext<OtherMessage>> _otherHandled;

        protected override void ConfigureRabbitMqReceiveEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
        {
            _otherHandled = Handled<OtherMessage>(configurator);
        }


        public class OtherMessage
        {
            public Guid CorrelationId { get; set; }
        }
    }


    [TestFixture]
    [Category("Flaky")]
    public class When_the_correlation_id_is_configured_explicitly :
        RabbitMqTestFixture
    {
        [Test]
        public async Task Should_use_the_explicitly_configured_correlation_id()
        {
            var transactionId = NewId.NextGuid();

            await InputQueueSendEndpoint.Send<ExplicitCorrelationMessage>(new { TransactionId = transactionId });

            ConsumeContext<ExplicitCorrelationMessage> explicitCorrelationContext = await _explicitCorrelationHandled;

            Assert.Multiple(() =>
            {
                Assert.That(explicitCorrelationContext.CorrelationId.HasValue, Is.True);
                Assert.That(explicitCorrelationContext.CorrelationId.Value, Is.EqualTo(transactionId));
            });
        }

        public When_the_correlation_id_is_configured_explicitly()
        {
            MessageCorrelation.UseCorrelationId<ExplicitCorrelationMessage>(x => x.TransactionId);
        }

        Task<ConsumeContext<ExplicitCorrelationMessage>> _explicitCorrelationHandled;

        protected override void ConfigureRabbitMqReceiveEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
        {
            _explicitCorrelationHandled = Handled<ExplicitCorrelationMessage>(configurator);
        }


        public class ExplicitCorrelationMessage
        {
            public Guid TransactionId { get; set; }
        }
    }


    [TestFixture]
    [Category("Flaky")]
    public class When_using_a_base_event :
        RabbitMqTestFixture
    {
        [Test]
        public async Task Should_handle_base_event_class()
        {
            var transactionId = NewId.NextGuid();

            await InputQueueSendEndpoint.Send<INewUserEvent>(new { TransactionId = transactionId });

            ConsumeContext<INewUserEvent> context = await _handled;

            Assert.Multiple(() =>
            {
                Assert.That(context.CorrelationId.HasValue, Is.True);
                Assert.That(context.CorrelationId.Value, Is.EqualTo(transactionId));
            });
        }

        Task<ConsumeContext<INewUserEvent>> _handled;

        protected override void ConfigureRabbitMqBus(IRabbitMqBusFactoryConfigurator configurator)
        {
            configurator.Send<IEvent>(x =>
            {
                x.UseCorrelationId(p => p.TransactionId);
            });
        }

        protected override void ConfigureRabbitMqReceiveEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
        {
            _handled = Handled<INewUserEvent>(configurator);
        }


        public interface IEvent
        {
            Guid TransactionId { get; }
        }


        public interface INewUserEvent :
            IEvent
        {
        }
    }
}
