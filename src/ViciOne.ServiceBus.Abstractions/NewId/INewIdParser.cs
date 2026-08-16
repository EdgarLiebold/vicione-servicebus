namespace ViciOne.ServiceBus
{
    public interface INewIdParser
    {
        NewId Parse(in string text);
    }
}
