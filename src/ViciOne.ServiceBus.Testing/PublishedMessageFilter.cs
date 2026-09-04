namespace ViciOne.ServiceBus.Testing;

public class PublishedMessageFilter
{
    readonly PublishedMessageFilterSet _excludes = new PublishedMessageFilterSet();
    readonly PublishedMessageFilterSet _includes = new PublishedMessageFilterSet();

    public PublishedMessageFilterSet Includes => _includes;

    public PublishedMessageFilterSet Excludes => _excludes;

    public bool Any(IPublishedMessage element)
    {
        return _includes.Any(element) && _excludes.None(element);
    }

    public bool None(IPublishedMessage element)
    {
        return _includes.None(element);
    }
}
