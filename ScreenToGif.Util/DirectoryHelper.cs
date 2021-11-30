using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ScreenToGif.Util;

public static class DirectoryHelper
{
    public static bool HasWriteRights(string directory)
    {
        try
        {
            //If the current folder does not exists yet, try getting the parent folder.
            while (!Directory.Exists(directory))
                directory = Path.GetDirectoryName(directory);

            //This will raise an exception if the path is read only or do not have access to view the permissions.
            return DirectoryHasPermission(directory, FileSystemRights.Write);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Test a directory for create file access permissions.
    /// </summary>
    /// <param name="directoryPath">Full path to directory </param>
    /// <param name="accessRight">File System right tested</param>
    /// <returns>State [bool]</returns>
    internal static bool DirectoryHasPermission(string directoryPath, FileSystemRights accessRight)
    {
        try
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            var rules = new DirectorySecurity(directoryPath, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group)
                .GetAccessRules(true, true, typeof(SecurityIdentifier)).OfType<FileSystemAccessRule>().OrderBy(o => o.AccessControlType == AccessControlType.Deny);
            //var rules = Directory.GetAccessControl(directoryPath).GetAccessRules(true, true, typeof(SecurityIdentifier)).OfType<FileSystemAccessRule>().OrderBy(o => o.AccessControlType == AccessControlType.Deny);

            foreach (var rule in rules)
            {
                //if (rule.IdentityReference as NTAccount == null)
                //    continue;

                //if (identity.Groups?.Contains(rule.IdentityReference) == true || identity.Owner?.Equals(rule.IdentityReference) == true || identity.User?.Equals(rule.IdentityReference) == true)
                if (rule.IdentityReference is SecurityIdentifier identifier && principal.IsInRole(identifier))
                {
                    if ((accessRight & rule.FileSystemRights) == accessRight)
                        return rule.AccessControlType == AccessControlType.Allow;
                }
            }

            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}