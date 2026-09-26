using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Internals.GraphValidation;

internal class Tarjan<T, TNode>
    where TNode : Node<T>, ITarjanNodeProperties
    where T : notnull
{
    readonly AdjacencyList<T, TNode> _list;
    readonly Stack<TNode> _stack;
    int _index;

    public Tarjan(AdjacencyList<T, TNode> list)
    {
        _list = list;
        _index = 0;
        Result = new List<IList<TNode>>();
        _stack = new Stack<TNode>();

        foreach (var node in _list.SourceNodes)
        {
            node.Index = -1;
            node.LowLink = -1;
        }

        foreach (var node in _list.SourceNodes)
        {
            if (node.Index != -1)
                continue;

            Compute(node);
        }
    }

    public IList<IList<TNode>> Result { get; }

    void Compute(TNode v)
    {
        v.Index = _index;
        v.LowLink = _index;
        _index++;

        _stack.Push(v);

        foreach (Edge<T, TNode> edge in _list.GetEdges(v))
        {
            var n = edge.Target;
            if (n.Index == -1)
            {
                Compute(n);
                v.LowLink = Math.Min(v.LowLink, n.LowLink);
            }
            else if (_stack.Contains(n))
                v.LowLink = Math.Min(v.LowLink, n.Index);
        }

        if (v.LowLink == v.Index)
        {
            TNode n;
            IList<TNode> component = new List<TNode>();
            do
            {
                n = _stack.Pop();
                component.Add(n);
            }
            while (!v.Equals(n));

            if (component.Count > 1 || _list.GetEdges(v).Any(edge => ReferenceEquals(edge.Target, v)))
                Result.Add(component);
        }
    }
}
