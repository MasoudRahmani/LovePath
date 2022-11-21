using LovePath.Interface;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace LovePath.Impersonation.Types
{
    /// <summary>
    ///  using the WindowsIdentity class to impersonate
    /// </summary>
    internal class WinIdentityType : IDisposable, IUserImpersonation
    {
        // Define the Windows LogonUser and CloseHandle functions.
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool LogonUser(String username, String domain, IntPtr password,
                int logonType, int logonProvider, ref IntPtr token);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);

        const int LOGON32_PROVIDER_DEFAULT = 0;
        const int LOGON32_LOGON_INTERACTIVE = 2;

        WindowsImpersonationContext wic;
        IntPtr tokenHandle;
        string _username;
        string _domain;
        IntPtr _passwordPtr;

        public WinIdentityType()
        {
        }
        public void Init(string username, string domain, SecureString password)
        {
            _username = username;
            _domain = domain;
            // Marshal the SecureString to unmanaged memory.
            _passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(password);
        }

        public bool ImpersonateValidUser()
        {
            try
            {
                bool returnValue = LogonUser(_username, _domain, _passwordPtr,
                LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT,
                ref tokenHandle);

                Debug.WriteLine("LogonUser called.");

                if (false == returnValue)
                {
                    Console.WriteLine("LogonUser failed with error code : {0}", Marshal.GetLastWin32Error());
                    return false;
                }

                Debug.WriteLine($"Did LogonUser Succeed? {(returnValue ? "Yes" : "No")}");
                Debug.WriteLine($"Value of Windows NT token: {tokenHandle}");

                // Check the identity.
                Debug.WriteLine($"Before impersonation: {WindowsIdentity.GetCurrent().Name}");

                // Use the token handle returned by LogonUser.
                var newId = new WindowsIdentity(tokenHandle);
                wic = newId.Impersonate();

                // Check the identity.
                Debug.WriteLine($"After impersonation: {WindowsIdentity.GetCurrent().Name}");

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Exception impersonating user: " + ex.Message);
            }
        }

        public bool RunImpersonated(Action action)
        {
            try
            {
                bool returnValue = LogonUser(_username, _domain, _passwordPtr,
                LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT,
                ref tokenHandle);

                if (false == returnValue)
                {
                    Console.WriteLine("LogonUser failed with error code : {0}", Marshal.GetLastWin32Error());
                    return false;
                }

                Debug.WriteLine($"Did LogonUser Succeed? {(returnValue ? "Yes" : "No")}");
                Debug.WriteLine($"Value of Windows NT token: {tokenHandle}");

                // Check the identity.
                Debug.WriteLine($"Before impersonation: {WindowsIdentity.GetCurrent().Name}");

                // Use the token handle returned by LogonUser.
                var newsd = new SafeAccessTokenHandle(tokenHandle);

                WindowsIdentity.RunImpersonated(newsd, action);

                // Check the identity.
                Debug.WriteLine($"After impersonation: {WindowsIdentity.GetCurrent().Name}");

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Exception impersonating user: " + ex.Message);
            }
        }

        public void Dispose()
        {
            // Perform cleanup whether or not the call succeeded.
            // Zero-out and free the unmanaged string reference.
            Marshal.ZeroFreeGlobalAllocUnicode(_passwordPtr);

            if (wic != null)
            {
                wic.Undo();
            }
            if (tokenHandle != IntPtr.Zero)
            {
                CloseHandle(tokenHandle);
            }
        }
    }
}
