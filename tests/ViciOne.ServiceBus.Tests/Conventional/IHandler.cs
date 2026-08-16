namespace ViciOne.ServiceBus.Tests.Conventional
{
    public interface IHandler<in T>
    {
        void Handle(T message);
    }
}
