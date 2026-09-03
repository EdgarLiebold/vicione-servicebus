namespace ViciOne.ServiceBus.Testing
{
    using System;
    using System.Threading.Tasks;
    using Implementations;


    public class HandlerTestHarness<TMessage>
        where TMessage : class
    {
        readonly ReceivedMessageList<TMessage> _consumed;
        readonly MessageHandler<TMessage> _handler;

        public HandlerTestHarness(BusTestHarness testHarness, MessageHandler<TMessage> handler)
        {
            _handler = handler;

            _consumed = new ReceivedMessageList<TMessage>(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
            ((ITestContextRetention)_consumed).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);

            testHarness.OnConfigureReceiveEndpoint += ConfigureReceiveEndpoint;
        }

        public IReceivedMessageList<TMessage> Consumed => _consumed;

        void ConfigureReceiveEndpoint(IReceiveEndpointConfigurator configurator)
        {
            configurator.Handler<TMessage>(HandleMessage);
        }

        async Task HandleMessage(ConsumeContext<TMessage> context)
        {
            try
            {
                await _handler(context).ConfigureAwait(false);

                _consumed.Add(context);
            }
            catch (Exception ex)
            {
                _consumed.Add(context, ex);
                throw;
            }
        }
    }
}
