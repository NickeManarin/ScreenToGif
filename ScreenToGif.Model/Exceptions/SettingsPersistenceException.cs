using System.Windows;

namespace ScreenToGif.Domain.Exceptions;

public sealed class SettingsPersistenceException : Exception
{
    public ResourceDictionary ResourceDictionary { get; }

    public bool IsLocal { get; }

    public SettingsPersistenceException()
    { }

    public SettingsPersistenceException(ResourceDictionary resourceDictionary, bool isLocal)
    {
        ResourceDictionary = resourceDictionary;
        IsLocal = isLocal;
    }
}