using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Util;

public static class IdentityHelper
{
    public static ApplicationTypes ApplicationType
    {
        get
        {
#if DEPENDANT_SINGLE
            //Dependent, Single File
            return ApplicationTypes.DependantSingle;
#elif FULL_SINGLE
            //Full, Single File
            return ApplicationTypes.FullSingle;
#elif FULL_MULTI_MSIX
            //Full, Multiple Files, MSIX
            return ApplicationTypes.FullMultiMsix;
#elif FULL_MULTI_MSIX_STORE
            //Full, Multiple Files, MSIX, Store
            return ApplicationTypes.FullMultiMsixStore;
#endif

            return ApplicationTypes.Unidentified;
        }
    }

    public static string ApplicationTypeDescription
    {
        get
        {
#if DEPENDANT_SINGLE
            return "Framework Dependent, Single File";
#elif FULL_SINGLE
            return "Full, Single File";
#elif FULL_MULTI_MSIX
            return "Full, Multiple Files, MSIX";
#elif FULL_MULTI_MSIX_STORE
            return "Full, Multiple Files, MSIX, Store";
#endif

            return "Unidentified";
        }
    }
}