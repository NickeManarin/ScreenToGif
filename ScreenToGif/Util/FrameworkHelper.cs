using System;
using Microsoft.Win32;

namespace ScreenToGif.Util
{
    internal class FrameworkHelper
    {
        /// <summary>
        /// Searchs for the current .Net Framework version installed.
        /// Code adapted from https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
        /// </summary>
        internal static string QueryFrameworkVersion()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            try
            {
                using (var sub = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
                {
                    if (!(sub?.GetValue("Release") is int key))
                        return "No 4.5 or later version detected";

                    //Checking the version using >= enables forward compatibility.
                    if (key >= 528040)
                        return "4.8 or later";
                    if (key >= 461808)
                        return "4.7.2";
                    if (key >= 461308)
                        return "4.7.1";
                    if (key >= 460798)
                        return "4.7";
                    if (key >= 394802)
                        return "4.6.2";
                    if (key >= 394254)
                        return "4.6.1";
                    if (key >= 393295)
                        return "4.6";
                    if (key >= 379893)
                        return "4.5.2";
                    if (key >= 378675)
                        return "4.5.1";
                    if (key >= 378389)
                        return "4.5";

                    //This code should never execute. A non-null release key should mean that 4.5 or later is installed.
                    return "No 4.5 or later version detected";
                }
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to detect .Net Framework version.");
                return "Not detectable";
            }
        }

        /// <summary>
        /// Searchs for the current .Net Framework version installed and returns true if has the necessary version installed.
        /// Code adapted from https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
        /// </summary>
        internal static bool HasFramework()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            try
            {
                using (var sub = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
                {
                    if (!(sub?.GetValue("Release") is int key))
                        return false;

                    //Has .Net 4.8 or newer. Checking the version using >= enables forward compatibility.
                    return key >= 528040;
                }
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to detect .Net Framework version.");
                return false;
            }
        }
    }
}