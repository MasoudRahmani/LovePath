using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security.AccessControl;
using System.Security.Principal;

namespace LovePath.Utility
{
    public static class SecurityUtil
    {
        public static string TranslateSid(SecurityIdentifier securityIdentifier)
        {
            string sidName = string.Empty;
            try
            {
                sidName = securityIdentifier.Translate(typeof(NTAccount)).ToString();
            }
            catch
            {
                Debug.WriteLine("failed to translate: " + securityIdentifier.ToString());
            }
            return sidName;
        }

        public static List<string> GetWellKnownSidsName()
        {
            List<string> sids = new List<string>();

            SecurityIdentifier sid;
            string sidName;
            foreach (WellKnownSidType sidType in Enum.GetValues(typeof(WellKnownSidType)))
            {
                try
                {
                    sid = new SecurityIdentifier(sidType, null);
                }
                catch
                {
                    Debug.WriteLine("failed to create: " + sidType.ToString());
                    continue;
                }
                sidName = TranslateSid(sid);

                if (string.IsNullOrEmpty(sidName) == false)
                    sids.Add(sidName);
            }
            return sids;
        }

        public static List<string> GetHumanUsers()
        {
            var localUsers = new List<string>();
            SelectQuery query = new SelectQuery("Win32_UserAccount");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject envVar in searcher.Get())
            {
                localUsers.Add((string)envVar["Name"]);
            }
            return localUsers;
        }

        public static bool AccountExists(string accountName)
        {
            bool bRet = false;
            try
            {
                NTAccount acct = new NTAccount(accountName);
                SecurityIdentifier id = (SecurityIdentifier)acct.Translate(typeof(SecurityIdentifier));

                bRet = id.IsAccountSid();
            }
            catch (IdentityNotMappedException)
            {
                //
            }
            return bRet;
        }

        public static AuthorizationRuleCollection GetFileAccessRule(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            FileSecurity fs = fi.GetAccessControl();

            return fs.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
        }

        public static void AllowFileAccessRule(string filePath, SecurityIdentifier fullAccountName, FileSystemRights fsr)
        {
            AllowFileAccessRule(filePath, TranslateSid(fullAccountName), fsr);
        }
        public static void AllowFileAccessRule(string filePath, WellKnownSidType knownSidType, FileSystemRights fsr)
        {
            var sid = new SecurityIdentifier(knownSidType, null);
            AllowFileAccessRule(filePath, sid, fsr);
        }
        public static void AllowFileAccessRule(string filePath, string fullAccountName, FileSystemRights fsr)
        {
            var fileSecurity = new FileInfo(filePath).GetAccessControl();

            fileSecurity.AddAccessRule(new FileSystemAccessRule(
                fullAccountName,
                fsr,
                InheritanceFlags.None,
                PropagationFlags.NoPropagateInherit,
                AccessControlType.Allow));

            //flush security access.
            File.SetAccessControl(filePath, fileSecurity);
        }

        /// <summary>
        /// Remove all Permission of a file or folder and set only the given user
        /// https://stackoverflow.com/questions/18740860/remove-all-default-file-permissions
        public static void ClearFileAccessRule(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);//get file info
            FileSecurity fs = fi.GetAccessControl();//get security access

            //remove any inherited access
            fs.SetAccessRuleProtection(true, false);

            //get any special user access
            AuthorizationRuleCollection rules = fs.GetAccessRules(true, true, typeof(NTAccount));

            //remove any special access
            foreach (FileSystemAccessRule rule in rules)
                fs.RemoveAccessRule(rule);

            File.SetAccessControl(filePath, fs);
        }

        public static List<string> GetUsersWithAccessOnFile(string path)
        {
            var rules = SecurityUtil.GetFileAccessRule(path);
            var validUsers = new List<string>();
            var humanUsers = SecurityUtil.GetHumanUsers();
            foreach (FileSystemAccessRule rule in rules)
            {
                var found = humanUsers.Find(x => x.Contains(rule.IdentityReference.Value.Split('\\')[1]));
                if (!string.IsNullOrWhiteSpace(found))
                {
                    if (FileSystemRights.FullControl == rule.FileSystemRights)
                        validUsers.Add(rule.IdentityReference.Value);
                }
            }
            return validUsers;
        }

        public static List<string> GetUsersWithAccessOnFile_slow(string path)
        {
            var rules = SecurityUtil.GetFileAccessRule(path);
            var validUsers = new List<string>();
            var wellknownacc = SecurityUtil.GetWellKnownSidsName();
            foreach (FileSystemAccessRule rule in rules)
            {
                var found = wellknownacc.Find(x => x.ToLowerInvariant().Contains(rule.IdentityReference.Value.ToLowerInvariant()));
                if (string.IsNullOrWhiteSpace(found))
                {
                    if (FileSystemRights.FullControl == rule.FileSystemRights)
                        validUsers.Add(rule.IdentityReference.Value);
                }
            }
            return validUsers;
        }

    }
}