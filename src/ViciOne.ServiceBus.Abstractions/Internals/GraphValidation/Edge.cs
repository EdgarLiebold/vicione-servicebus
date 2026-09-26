namespace ViciOne.ServiceBus.Internals.GraphValidation;

internal struct Edge<T, TNode>
    where TNode : Node<T>
{
    public readonly TNode Source;
    public readonly TNode Target;
    public readonly int Weight;

    public Edge(TNode source, TNode target, int weight)
    {
        Source = source;
        Target = target;
        Weight = weight;
    }
}
