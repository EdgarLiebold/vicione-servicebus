using System.Collections.Generic;
using System.Text;

namespace ViciOne.ServiceBus.Internals.GraphValidation;

internal class DependencyGraph<T>
    where T : notnull
{
    readonly AdjacencyList<T, DependencyGraphNode<T>> _adjacencyList;

    public DependencyGraph(int capacity)
    {
        _adjacencyList = new AdjacencyList<T, DependencyGraphNode<T>>(DefaultNodeFactory, capacity);
    }

    static DependencyGraphNode<T> DefaultNodeFactory(int index, T value)
    {
        return new DependencyGraphNode<T>(index, value);
    }

    public void Add(T source, T target)
    {
        _adjacencyList.AddEdge(source, target, 0);
    }

    public void EnsureGraphIsAcyclic()
    {
        var tarjan = new Tarjan<T, DependencyGraphNode<T>>(_adjacencyList);

        if (tarjan.Result.Count == 0)
            return;

        var message = new StringBuilder();
        foreach (IList<DependencyGraphNode<T>> cycle in tarjan.Result)
        {
            message.Append("(");
            for (var i = 0; i < cycle.Count; i++)
            {
                if (i > 0)
                    message.Append(",");

                message.Append(cycle[i].Value);
            }

            message.Append(")");
        }

        throw new CyclicGraphException("The dependency graph contains cycles: " + message);
    }
}
