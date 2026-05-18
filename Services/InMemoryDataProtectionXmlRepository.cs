using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace Molina.Bedding.Mvc.Services;

public sealed class InMemoryDataProtectionXmlRepository : IXmlRepository
{
    private readonly List<XElement> _elements = [];
    private readonly object _syncRoot = new();

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        lock (_syncRoot)
        {
            return _elements.Select(static element => new XElement(element)).ToList();
        }
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        lock (_syncRoot)
        {
            _elements.Add(new XElement(element));
        }
    }
}
