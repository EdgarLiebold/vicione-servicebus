namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Contracts
{
    public interface OnionRingsCompleted :
        OrderLineCompleted
    {
        int Quantity { get; }
    }
}
