using LovePath.Interface;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace LovePath.Impersonation.Types
{
    /// <summary>
    ///  using the Win32 APIs to impersonate
    /// </summary>
    internal class Win32Type : IDisposable, IUserImpersonation
    {
        // Define the Windows LogonUser and CloseHandle functions.
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool LogonUser(String username, String domain, IntPtr password,
                int logonType, int logonProvider, ref IntPtr token);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int DuplicateToken(IntPtr hToken,
            int impersonationLevel,
            ref IntPtr hNewToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RevertToSelf();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);

        const int LOGON32_PROVIDER_DEFAULT = 0;
        const int LOGON32_LOGON_INTERACTIVE = 2;

        WindowsImpersonationContext wic;
        string _userName;
        string _domain;
        IntPtr _passwordPtr;
        IntPtr token = IntPtr.Zero;
        IntPtr tokenDuplicate = IntPtr.Zero;

        public Win32Type()
        {
        }
        public void Init(string username, string domain, SecureString password)
        {
            _userName = username;
            _domain = domain;
            // Marshal the SecureString to unmanaged memory.
            _passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(password);
        }

        public bool ImpersonateValidUser()
        {
            WindowsIdentity wi;

            if (RevertToSelf())
            {
                if (LogonUser(_userName, _domain, _passwordPtr, LOGON32_LOGON_INTERACTIVE,
                    LOGON32_PROVIDER_DEFAULT, ref token) != false)
                {
                    if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
                    {
                        wi = new WindowsIdentity(tokenDuplicate);
                        wic = wi.Impersonate();
                        if (wic != null)
                        {
                            CloseHandle(token);
                            CloseHandle(tokenDuplicate);
                            return true;
                        }
                    }
                }
            }

            if (token != IntPtr.Zero)
            {
                CloseHandle(token);
            }
            if (tokenDuplicate != IntPtr.Zero)
            {
                CloseHandle(tokenDuplicate);
            }
            return false;
        }

        public bool RunImpersonated(Action action)
        {
            bool result = false;
            if (RevertToSelf())
            {
                if (LogonUser(_userName, _domain, _passwordPtr, LOGON32_LOGON_INTERACTIVE,
                    LOGON32_PROVIDER_DEFAULT, ref token))
                {
                    if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
                    {
                        // Use the token handle returned by LogonUser.
                        var newsd = new SafeAccessTokenHandle(tokenDuplicate);
                        WindowsIdentity.RunImpersonated(newsd, action);
                        result = true;
                    }
                }
            }
            if (token != IntPtr.Zero)
            {
                CloseHandle(token);
            }
            if (tokenDuplicate != IntPtr.Zero)
            {
                CloseHandle(tokenDuplicate);
            }

            return result;
        }

        public void Dispose()
        {
            // Perform cleanup whether or not the call succeeded.
            // Zero-out and free the unmanaged string reference.
            Marshal.ZeroFreeGlobalAllocUnicode(_passwordPtr);
            if (wic != null)
            {
                wic.Dispose();
            }
            RevertToSelf();
        }


    }

}
