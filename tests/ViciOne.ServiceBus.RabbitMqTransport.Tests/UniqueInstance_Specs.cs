// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Configuration;
    using NUnit.Framework;
    using Testing;


    [TestFixture]
    public class Specifying_a_unique_instance_for_an_endpoint
    {
        [Test]
        public async Task Should_fan_out_published_messages()
        {
            // Both harnesses need their own input queue. With the default name they consume from the
            // shared 'input_queue', and because their buses recover into the virtual host that the next
            // fixture recreates, that fixture's own consumer ends up competing with them. RabbitMQ then
            // hands a message to whichever consumer is next in turn; when it lands on the leftover one
            // it is discarded, and the waiting fixture never sees it. Measured: this fixture followed by
            // the Turnout scenario is red with two consumers on input_queue, and green with none.
            var firstHarness = new RabbitMqTestHarness("unique-instance-first");
            var firstConsumer = new EventConsumer(firstHarness.GetTask<ConsumeContext<SomeEvent>>());
            firstHarness.OnConfigureRabbitMqBus += configurator =>
            {
                var endpointSettings = new EndpointSettings<IEndpointDefinition<EventConsumer>> { InstanceId = "27" };
                var endpointDefinition = new ConsumerEndpointDefinition<EventConsumer>(endpointSettings);

                configurator.ReceiveEndpoint(endpointDefinition, KebabCaseEndpointNameFormatter.Instance, e =>
                {
                    e.Consumer(() => firstConsumer);
                });
            };

            await firstHarness.Start();
            try
            {
                var secondHarness = new RabbitMqTestHarness("unique-instance-second");
                var secondConsumer = new EventConsumer(secondHarness.GetTask<ConsumeContext<SomeEvent>>());
                secondHarness.OnConfigureRabbitMqBus += configurator =>
                {
                    var endpointSettings = new EndpointSettings<IEndpointDefinition<EventConsumer>> { InstanceId = "42" };
                    var endpointDefinition = new ConsumerEndpointDefinition<EventConsumer>(endpointSettings);

                    configurator.ReceiveEndpoint(endpointDefinition, KebabCaseEndpointNameFormatter.Instance, e =>
                    {
                        e.Consumer(() => secondConsumer);
                    });
                };

                await secondHarness.Start();
                try
                {
                    await firstHarness.Bus.Publish(new SomeEvent());

                    await firstConsumer.Completed;

                    await secondConsumer.Completed;
                }
                finally
                {
                    await secondHarness.Stop();
                }
            }
            finally
            {
                await firstHarness.Stop();
            }
        }


        class EventConsumer
            : IConsumer<SomeEvent>
        {
            readonly TaskCompletionSource<ConsumeContext<SomeEvent>> _source;

            public EventConsumer(TaskCompletionSource<ConsumeContext<SomeEvent>> source)
            {
                _source = source;
            }

            public Task<ConsumeContext<SomeEvent>> Completed => _source.Task;

            public Task Consume(ConsumeContext<SomeEvent> context)
            {
                _source.TrySetResult(context);

                return Task.CompletedTask;
            }
        }
    }


    public class SomeEvent
    {
    }
}
