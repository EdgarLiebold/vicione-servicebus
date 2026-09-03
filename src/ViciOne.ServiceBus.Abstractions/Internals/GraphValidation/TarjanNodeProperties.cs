namespace ViciOne.ServiceBus.Internals.GraphValidation
{
    internal interface ITarjanNodeProperties
    {
        int Index { get; set; }
        int LowLink { get; set; }
    }
}
