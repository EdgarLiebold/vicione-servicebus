namespace ViciOne.ServiceBus.Internals.GraphValidation
{
    public interface ITarjanNodeProperties
    {
        int Index { get; set; }
        int LowLink { get; set; }
    }
}
