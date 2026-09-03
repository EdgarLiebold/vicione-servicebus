namespace ViciOne.ServiceBus.Internals.GraphValidation
{
    internal interface ITopologicalSortNodeProperties
    {
        bool Visited { get; set; }
    }
}
