namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Contracts
{
    public interface OrderFryShake :
        OrderLine
    {
        string Flavor { get; }
        Size Size { get; }
    }
}
