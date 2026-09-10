using System.Collections.Generic;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Represents the topic topology entity.</summary>
public class TopicEntity :
    Topic,
    TopicHandle
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="id">The id.</param>
    /// <param name="name">The name.</param>
    public TopicEntity(long id, string name)
    {
        Id = id;
        TopicName = name;
    }

    /// <summary>Gets the name comparer.</summary>
    public static IEqualityComparer<TopicEntity> NameComparer { get; } = new NameEqualityComparer();
    /// <summary>Gets the entity comparer.</summary>
    public static IEqualityComparer<TopicEntity> EntityComparer { get; } = new NameEqualityComparer();

    /// <summary>Gets the topic name.</summary>
    public string TopicName { get; }
    /// <summary>Gets the id.</summary>
    public long Id { get; }
    /// <summary>Gets the topic.</summary>
    public Topic Topic => this;

    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
    public override string ToString()
    {
        return TopicName;
    }


    sealed class NameEqualityComparer : IEqualityComparer<TopicEntity>
    {
        public bool Equals(TopicEntity? x, TopicEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;
            return string.Equals(x.TopicName, y.TopicName);
        }

        public int GetHashCode(TopicEntity obj)
        {
            return obj.TopicName.GetHashCode();
        }
    }
}
