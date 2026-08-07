// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Internals.GraphValidation
{
    using System;


    public class DependencyGraphNode<T> :
        Node<T>,
        ITopologicalSortNodeProperties,
        ITarjanNodeProperties,
        IComparable<DependencyGraphNode<T>>
    {
        public DependencyGraphNode(int index, T value)
            : base(index, value)
        {
            Visited = false;
            LowLink = -1;
            Index = -1;
        }

        public int Index { get; set; }
        public int LowLink { get; set; }
        public bool Visited { get; set; }
    }
}
