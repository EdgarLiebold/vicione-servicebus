namespace ViciOne.ServiceBus.Internals.GraphValidation;

internal class DependencyGraphNode<T> :
    Node<T>,
    ITarjanNodeProperties
{
    public DependencyGraphNode(int index, T value)
        : base(index, value)
    {
        LowLink = -1;
        Index = -1;
    }

    public int Index { get; set; }
    public int LowLink { get; set; }
}
