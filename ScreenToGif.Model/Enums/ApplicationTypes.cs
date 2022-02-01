namespace ScreenToGif.Domain.Enums;

public enum ApplicationTypes
{
    Unidentified = 0,

    /// <summary>
    /// Light package (.NET 6 desktop runtime download and installation required).
    /// Distributted as a single file, as an EXE.
    /// </summary>
    DependantSingle = 1,

    /// <summary>
    /// Full package (.NET 6 desktop runtime included).
    /// Distributted as a single file, as an EXE.
    /// </summary>
    FullSingle = 2,

    /// <summary>
    /// Full package (.NET 6 desktop runtime included).
    /// Distributted as multiple files, as a MSIX for the outside the Store.
    /// </summary>
    FullMultiMsix = 3,

    /// <summary>
    /// Full package (.NET 6 desktop runtime included).
    /// Distributted as multiple files, as a MSIX for the Store.
    /// </summary>
    FullMultiMsixStore = 4,
}