using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>Preserves a logical destination address while dispatching through the local mediator endpoint.</summary>
internal sealed class AddressedMediatorSendEndpoint :
    SendEndpointProxy
{
    readonly Uri _destinationAddress;

    public AddressedMediatorSendEndpoint(ISendEndpoint endpoint, Uri destinationAddress)
        : base(endpoint)
    {
        _destinationAddress = destinationAddress ?? throw new ArgumentNullException(nameof(destinationAddress));
        if (!destinationAddress.IsAbsoluteUri)
            throw new ArgumentException("A mediator destination address must be absolute.", nameof(destinationAddress));
    }

    protected override IPipe<SendContext<T>> GetPipeProxy<T>(IPipe<SendContext<T>>? pipe = default)
    {
        return new DestinationAddressPipe<T>(_destinationAddress, pipe);
    }

    sealed class DestinationAddressPipe<TMessage> :
        SendContextPipeAdapter<TMessage>
        where TMessage : class
    {
        readonly Uri _destinationAddress;

        public DestinationAddressPipe(Uri destinationAddress, IPipe<SendContext<TMessage>>? pipe)
            : base(pipe)
        {
            _destinationAddress = destinationAddress;
        }

        protected override void Send(SendContext<TMessage> context)
        {
            context.DestinationAddress = _destinationAddress;
        }

        protected override void Send<T>(SendContext<T> context)
        {
            context.DestinationAddress = _destinationAddress;
        }
    }
}
