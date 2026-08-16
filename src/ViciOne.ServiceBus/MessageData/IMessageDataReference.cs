namespace ViciOne.ServiceBus.MessageData
{
    public interface IMessageDataReference
    {
        string Text { set; }
        byte[] Data { set; }
    }
}
