namespace ViciOne.ServiceBus
{
    public interface INewIdFormatter
    {
        string Format(in byte[] bytes);
    }
}
