using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace LovePath
{
    //https://stackoverflow.com/questions/1192631/open-a-shared-file-under-another-user-and-domain

    /// <summary>
    ///  using the Win32 APIs to impersonate
    /// </summary>
    class UserImpersonation : IDisposable
    {
        [DllImport("advapi32.dll")]
        public static extern int LogonUser(String lpszUserName,
            String lpszDomain,
            String lpszPassword,
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

        public UserImpersonation(string userName, string domain, string passWord)
        {
            _userName = userName;
            _domain = domain;
            _passWord = passWord;
        }

        public bool ImpersonateValidUser()
        {
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
    class UserImpersonation2 : IDisposable
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

        public UserImpersonation2()
        {

        }
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
            bool returnValue = LogonUser(_userName, _domain, _passWord,
                    LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT,
                    ref tokenHandle);

            Console.WriteLine("LogonUser called.");

            if (false == returnValue)
            {
                int ret = Marshal.GetLastWin32Error();
                Console.WriteLine("LogonUser failed with error code : {0}", ret);
                return false;
            }

            Console.WriteLine("Did LogonUser Succeed? " + (returnValue ? "Yes" : "No"));
            Console.WriteLine("Value of Windows NT token: " + tokenHandle);

            // Check the identity.
            Console.WriteLine("Before impersonation: "
                + WindowsIdentity.GetCurrent().Name);

            // Use the token handle returned by LogonUser.
            WindowsIdentity newId = new WindowsIdentity(tokenHandle);
            wic = newId.Impersonate();

            // Check the identity.
            Console.WriteLine("After impersonation: "
                + WindowsIdentity.GetCurrent().Name);

            return true;
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
}
