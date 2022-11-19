using LovePath.Interface;
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

            Type t = null;
            _userImpersonationObj = (IUserImpersonation)GetUserImpersonationInstance(impersonationType, ref t);
        }
        public bool RunImpersonated(Action action)
        {
            if (_userImpersonationObj.ImpersonateValidUser(_user, _doman, _pass))
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

        public object GetUserImpersonationInstance(ImpersonationType impersonationType, ref Type impType)
        {
            Type t;
            switch (impersonationType)
            {
                case ImpersonationType.UserImpersonation:
                    impType = t = typeof(UserImpersonation);
                    return Activator.CreateInstance(t);

                case ImpersonationType.UserImpersonation2:
                    impType = t = typeof(UserImpersonation2);
                    return Activator.CreateInstance(t);

                default:
                    throw new Exception("Invalid Input");
            }

        }


        #region USER Impersonation with p/Invoke
        //https://stackoverflow.com/questions/1192631/open-a-shared-file-under-another-user-and-domain
        /// <summary>
        ///  using the Win32 APIs to impersonate
        /// </summary>
        private class UserImpersonation : IDisposable, Interface.IUserImpersonation
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

            public void Init(string userName, string domain, string passWord)
            {
                _userName = userName;
                _domain = domain;
                _passWord = passWord;
            }

            public bool ImpersonateValidUser(string account, string domain, string password)
            {
                Init(account, domain, password);
                WindowsIdentity wi;
                IntPtr token = IntPtr.Zero;
                IntPtr tokenDuplicate = IntPtr.Zero;

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

            #region IDisposable Members
            public void Dispose()
            {
                if (wic != null)
                {
                    wic.Dispose();
                }
                RevertToSelf();
            }
            #endregion
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

            public void Init(string userName, string domain, string passWord)
            {
                _userName = userName;
                _domain = domain;
                _passWord = passWord;
            }

            const int LOGON32_PROVIDER_DEFAULT = 0;
            const int LOGON32_LOGON_INTERACTIVE = 2;

            public bool ImpersonateValidUser(string userName, string domain, string passWord)
            {
                Init(userName, domain, passWord);
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

            #region IDisposable Members
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
            #endregion
        }
        #endregion

    }
}
