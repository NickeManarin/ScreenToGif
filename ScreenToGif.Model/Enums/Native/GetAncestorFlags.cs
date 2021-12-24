namespace ScreenToGif.Domain.Enums.Native;

public enum GetAncestorFlags
{
    /// <summary>
    /// Retrieves the parent window. This does not include the owner, as it does with the GetParent function. 
    /// </summary>
    GetParent = 1,
    /// <summary>
    /// Retrieves the root window by walking the chain of parent windows.
    /// </summary>
    GetRoot = 2,
    /// <summary>
    /// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent. 
    /// </summary>
    GetRootOwner = 3
}