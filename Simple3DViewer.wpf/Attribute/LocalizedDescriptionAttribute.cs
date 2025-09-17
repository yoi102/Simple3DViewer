using System.ComponentModel;
using System.Resources;

namespace Simple3DViewer.wpf.Attribute;

public class LocalizedDescriptionAttribute : DescriptionAttribute
{
    readonly ResourceManager _resourceManager;
    readonly string _resourceKey;

    public LocalizedDescriptionAttribute(string resourceKey, Type resourceType)
    {
        _resourceManager = new ResourceManager(resourceType);
        _resourceKey = resourceKey;
    }

    public override string Description
    {
        get
        {
            var description = _resourceManager.GetString(_resourceKey);
            return string.IsNullOrWhiteSpace(description) ? string.Format("[[{0}]]", _resourceKey) : description;
        }
    }
}
