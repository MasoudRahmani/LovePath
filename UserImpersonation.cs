using LovePath.Interface;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace LovePath
{
    // If any Type or Method is added here you need to add how to run it in "GetUserImpersonationInstance"
    enum ImpersonationType
    {
        UserImpersonation,
        UserImpersonation2
    }

    class ImpersonateUser
    {
        private string _doman;
        private string _user;
        private SecureString _pass;
        private IUserImpersonation _userImpersonationObj;

        public ImpersonateUser(ImpersonationType impersonationType, string domain, string user, SecureString pass)
        {
            _doman = domain;
            _user = user;
            _pass = pass;

            _userImpersonationObj = (IUserImpersonation)GetUserImpersonationInstance(impersonationType);
        }
        public bool RunImpersonated_Old(Action action)
        {
            if (_userImpersonationObj.ImpersonateValidUser())
            {
                action();
            }
            else
            {
                _userImpersonationObj.Dispose();
                return false;
            }

            _userImpersonationObj.Dispose();
            return true;
        }
        public bool RunImpersonated(Action action)
        {
            return _userImpersonationObj.RunImpersonated(action);
        }
        public object GetUserImpersonationInstance(ImpersonationType impersonationType)
        {
            switch (impersonationType)
            {
                case ImpersonationType.UserImpersonation:
                    return new UserImpersonation(_user, _doman, _pass);

                case ImpersonationType.UserImpersonation2:

                    return new UserImpersonation2(_user, _doman, _pass);

                default:
                    throw new Exception("Invalid Input");
            }

        }


        #region USER Impersonation with p/Invoke
        //https://stackoverflow.com/questions/1192631/open-a-shared-file-under-another-user-and-domain
        /// <summary>
        ///  using the Win32 APIs to impersonate
        /// </summary>
        private class UserImpersonation : IDisposable, IUserImpersonation
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

            public UserImpersonation(string userName, string domain, SecureString passWord)
            {
                _userName = userName;
                _domain = domain;
                // Marshal the SecureString to unmanaged memory.
                _passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(passWord);
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

        /// <summary>
        ///  using the WindowsIdentity class to impersonate
        /// </summary>
        private class UserImpersonation2 : IDisposable, IUserImpersonation
        {
            // Define the Windows LogonUser and CloseHandle functions.
            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern bool LogonUser(String username, String domain, IntPtr password,
                    int logonType, int logonProvider, ref IntPtr token);

            //[DllImport("advapi32.dll")]
            //public static extern bool LogonUser(String lpszUserName,
            //    String lpszDomain,
            //    String lpszPassword,
            //    int dwLogonType,
            //    int dwLogonProvider,
            //    ref IntPtr phToken);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            public static extern bool CloseHandle(IntPtr handle);

            WindowsImpersonationContext wic;
            IntPtr tokenHandle;
            string _userName;
            string _domain;
            IntPtr _passwordPtr;

            public UserImpersonation2(string userName, string domain, SecureString passWord)
            {
                _userName = userName;
                _domain = domain;
                // Marshal the SecureString to unmanaged memory.
                _passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(passWord);
            }

            const int LOGON32_PROVIDER_DEFAULT = 0;
            const int LOGON32_LOGON_INTERACTIVE = 2;

            public bool ImpersonateValidUser()
            {
                try
                {
                    bool returnValue = LogonUser(_userName, _domain, _passwordPtr,
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
                    bool returnValue = LogonUser(_userName, _domain, _passwordPtr,
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
        #endregion
    }
}