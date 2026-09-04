using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a handler test harness implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class HandlerTestHarness<TMessage>
    where TMessage : class
{
    readonly ReceivedMessageList<TMessage> _consumed;
    readonly MessageHandler<TMessage> _handler;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="testHarness">The test harness value.</param>
    /// <param name="handler">The handler value.</param>
    public HandlerTestHarness(BusTestHarness testHarness, MessageHandler<TMessage> handler)
    {
        _handler = handler;

        _consumed = new ReceivedMessageList<TMessage>(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
        ((ITestContextRetention)_consumed).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);

        testHarness.OnConfigureReceiveEndpoint += ConfigureReceiveEndpoint;
    }

    /// <summary>
    /// Gets the consumed value.
    /// </summary>
    public IReceivedMessageList<TMessage> Consumed => _consumed;

    void ConfigureReceiveEndpoint(IReceiveEndpointConfigurator configurator)
    {
        configurator.Handler<TMessage>(HandleMessageAsync);
    }

    async Task HandleMessageAsync(ConsumeContext<TMessage> context)
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
