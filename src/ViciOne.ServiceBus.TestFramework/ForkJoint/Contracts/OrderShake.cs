namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Contracts
{
    public interface OrderShake :
        OrderLine
    {
        string Flavor { get; }
        Size Size { get; }
    }
}
