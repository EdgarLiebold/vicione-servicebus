using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Registers a message handler under test and records its successful and faulted deliveries.</summary>
/// <typeparam name="TMessage">The handled message contract.</typeparam>
public sealed class HandlerTestHarness<TMessage>
    where TMessage : class
{
    readonly ReceivedMessageList<TMessage> _consumed;
    readonly MessageHandler<TMessage> _handler;

    /// <summary>Registers a message handler with the harness's default receive endpoint.</summary>
    /// <param name="testHarness">The bus harness that hosts the handler.</param>
    /// <param name="handler">The handler to invoke for each matching message.</param>
    public HandlerTestHarness(BusTestHarness testHarness, MessageHandler<TMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(testHarness);
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;

        _consumed = new ReceivedMessageList<TMessage>(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
        ((ITestContextRetention)_consumed).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);

        testHarness.ReceiveEndpointConfiguring += ConfigureReceiveEndpoint;
    }

    /// <summary>Gets successful and faulted deliveries observed by the handler.</summary>
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
