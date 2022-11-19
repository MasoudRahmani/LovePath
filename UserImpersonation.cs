using LovePath.Interface;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
        private string _pass;
        private IUserImpersonation _userImpersonationObj;

        public ImpersonateUser(ImpersonationType impersonationType, string domain, string user, string pass)
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
            [DllImport("advapi32.dll")]
            public static extern int LogonUser(string lpszUserName,
                string lpszDomain,
                string lpszPassword,
                int dwLogonType,
                int dwLogonProvider,
                ref IntPtr phToken);

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
            string _passWord;
            IntPtr token = IntPtr.Zero;
            IntPtr tokenDuplicate = IntPtr.Zero;

            public UserImpersonation(string userName, string domain, string passWord)
            {
                _userName = userName;
                _domain = domain;
                _passWord = passWord;
            }

            public bool ImpersonateValidUser()
            {
                WindowsIdentity wi;

                if (RevertToSelf())
                {
                    if (LogonUser(_userName, _domain, _passWord, LOGON32_LOGON_INTERACTIVE,
                        LOGON32_PROVIDER_DEFAULT, ref token) != 0)
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
                    if (LogonUser(_userName, _domain, _passWord, LOGON32_LOGON_INTERACTIVE,
                        LOGON32_PROVIDER_DEFAULT, ref token) != 0)
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
        private class UserImpersonation2 : IDisposable, Interface.IUserImpersonation
        {
            [DllImport("advapi32.dll")]
            public static extern bool LogonUser(String lpszUserName,
                String lpszDomain,
                String lpszPassword,
                int dwLogonType,
                int dwLogonProvider,
                ref IntPtr phToken);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            public static extern bool CloseHandle(IntPtr handle);

            WindowsImpersonationContext wic;
            IntPtr tokenHandle;
            string _userName;
            string _domain;
            string _passWord;

            public UserImpersonation2(string userName, string domain, string passWord)
            {
                _userName = userName;
                _domain = domain;
                _passWord = passWord;
            }

            const int LOGON32_PROVIDER_DEFAULT = 0;
            const int LOGON32_LOGON_INTERACTIVE = 2;

            public bool ImpersonateValidUser()
            {
                try
                {
                    bool returnValue = LogonUser(_userName, _domain, _passWord,
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
                    bool returnValue = LogonUser(_userName, _domain, _passWord,
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
