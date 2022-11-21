using LovePath.Impersonation.Types;
using LovePath.Interface;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
//https://stackoverflow.com/questions/1192631/open-a-shared-file-under-another-user-and-domain


public enum ImpersonationType
{
    //Name has to math actual class
    Win32,
    WinIdentity
}

namespace LovePath.Impersonation
{

    public class ImpersonateUser
    {
        public string ImpersonationTypeNamespacePath = "LovePath.Impersonation.Types.";
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

        public object GetUserImpersonationInstance(ImpersonationType chosenType)
        {
            string typeToCreateName = Enum.GetName(typeof(ImpersonationType), chosenType);

            var allAssemblyTypes = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, AppDomain.CurrentDomain.FriendlyName)).GetTypes();

            object obj = null;

            foreach (var type in allAssemblyTypes)
            {
                if (type.FullName.Contains(ImpersonationTypeNamespacePath) &
                    type.Name.Contains(typeToCreateName))
                {
                    obj = Utility.Util.GetInstanceofType2(type.FullName);
                    break;
                }
            }
            var imp = (IUserImpersonation)obj;
            imp.Init(_user, _doman, _pass);
            return imp;
            //switch (chosenType)
            //{
            //    case ImpersonationType.Win32:
            //        var f = new Win32Type();
            //        f.Init(_user, _doman, _pass);
            //        return f;
            //    case ImpersonationType.WinIdentity:
            //        var hu = new WinIdentityType();
            //        hu.Init(_user, _doman, _pass);
            //        return hu;

            //    default:
            //        var b = new WinIdentityType();
            //        b.Init(_user, _doman, _pass);
            //        return b;
            //}

        }

        public bool RunWithImpersonatingValidUser(Action action)
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

        public bool RunImpersonatedDirectly(Action action)
        {
            return _userImpersonationObj.RunImpersonated(action);
        }
    }
}



class Tester
{
    //[DllImport("advapi32", SetLastError = true),
    //SuppressUnmanagedCodeSecurityAttribute]
    //static extern int OpenProcessToken(
    //IntPtr ProcessHandle,
    //int DesiredAccess,
    //ref IntPtr TokenHandle
    //);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool OpenProcessToken(
        IntPtr ProcessHandle,// handle to process
        uint DesiredAccess,// desired access to process
        out IntPtr TokenHandle);// handle to open access token

    [DllImport("kernel32", SetLastError = true),
    SuppressUnmanagedCodeSecurityAttribute]
    static extern bool CloseHandle(IntPtr handle);
    [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public extern static bool DuplicateToken(IntPtr ExistingTokenHandle,
    int SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);

    //Use these for DesiredAccess
    public const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
    public const uint STANDARD_RIGHTS_READ = 0x00020000;
    public const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
    public const uint TOKEN_DUPLICATE = 0x0002;
    public const uint TOKEN_IMPERSONATE = 0x0004;
    public const uint TOKEN_QUERY = 0x0008;
    public const uint TOKEN_QUERY_SOURCE = 0x0010;
    public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    public const uint TOKEN_ADJUST_GROUPS = 0x0040;
    public const uint TOKEN_ADJUST_DEFAULT = 0x0080;
    public const uint TOKEN_ADJUST_SESSIONID = 0x0100;
    public const uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
    public const uint TOKEN_ALL_ACCESS = (
        STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
        TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY |
        TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES |
        TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID);

    IntPtr hToken = IntPtr.Zero;
    IntPtr dupeTokenHandle = IntPtr.Zero;

    Tester()
    {
        // For simplicity I'm using the PID of System here
        Process proc = Process.GetProcessById(4);
        if (OpenProcessToken(proc.Handle,
        TOKEN_QUERY | TOKEN_IMPERSONATE | TOKEN_DUPLICATE,
        out hToken))
        {
            WindowsIdentity newId = new WindowsIdentity(hToken);
            Console.WriteLine(newId.Owner);
            try
            {
                const int SecurityImpersonation = 2;
                dupeTokenHandle = DupeToken(hToken,
                SecurityImpersonation);
                if (IntPtr.Zero == dupeTokenHandle)
                {
                    string s = String.Format("Dup failed {0}, privilege not held",
                    Marshal.GetLastWin32Error());
                    throw new Exception(s);
                }

                WindowsImpersonationContext impersonatedUser =
                newId.Impersonate();
                IntPtr accountToken = WindowsIdentity.GetCurrent().Token;
                Console.WriteLine("Token number is: " + accountToken.ToString());
                Console.WriteLine("Windows ID Name is: " +
                WindowsIdentity.GetCurrent().Name);
            }
            finally
            {
                CloseHandle(hToken);
            }
        }
        else
        {
            string s = String.Format("OpenProcess Failed {0}, privilege not held", Marshal.GetLastWin32Error());
            throw new Exception(s);
        }
    }
    IntPtr DupeToken(IntPtr token, int Level)
    {
        IntPtr dupeTokenHandle = IntPtr.Zero;
        bool retVal = DuplicateToken(token, Level, ref dupeTokenHandle);
        return dupeTokenHandle;
    }

    //https://www.pinvoke.net/

    public void Example_GetSidByteArr()
    {
        Process[] myProcesses = Process.GetProcesses();
        foreach (Process myProcess in myProcesses)
        {
            byte[] sidBytes = GetSIDByteArr(myProcess.Handle);
        }
    }

    public static byte[] GetSIDByteArr(IntPtr processHandle)
    {
        int MAX_INTPTR_BYTE_ARR_SIZE = 512;
        IntPtr tokenHandle;
        byte[] sidBytes;

        // Get the Process Token
        if (!OpenProcessToken(processHandle, TOKEN_READ, out tokenHandle))
            throw new ApplicationException("Could not get process token.  Win32 Error Code: " + Marshal.GetLastWin32Error());

        uint tokenInfoLength = 0;
        bool result;
        result = GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, tokenInfoLength, out tokenInfoLength);  // get the token info length
        IntPtr tokenInfo = Marshal.AllocHGlobal((int)tokenInfoLength);
        result = GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, tokenInfo, tokenInfoLength, out tokenInfoLength);  // get the token info

        // Get the User SID
        if (result)
        {
            TOKEN_USER tokenUser = (TOKEN_USER)Marshal.PtrToStructure(tokenInfo, typeof(TOKEN_USER));
            sidBytes = new byte[MAX_INTPTR_BYTE_ARR_SIZE];  // Since I don't yet know how to be more precise w/ the size of the byte arr, it is being set to 512
            Marshal.Copy(tokenUser.User.Sid, sidBytes, 0, MAX_INTPTR_BYTE_ARR_SIZE);  // get a byte[] representation of the SID
        }
        else throw new ApplicationException("Could not get process token.  Win32 Error Code: " + Marshal.GetLastWin32Error());

        return sidBytes;
    }
    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

}
public struct TOKEN_USER
{
    public SID_AND_ATTRIBUTES User;
}

[StructLayout(LayoutKind.Sequential)]
public struct SID_AND_ATTRIBUTES
{

    public IntPtr Sid;
    public int Attributes;
}

public enum TOKEN_INFORMATION_CLASS
{
    TokenUser = 1,
    TokenGroups,
    TokenPrivileges,
    TokenOwner,
    TokenPrimaryGroup,
    TokenDefaultDacl,
    TokenSource,
    TokenType,
    TokenImpersonationLevel,
    TokenStatistics,
    TokenRestrictedSids,
    TokenSessionId,
    TokenGroupsAndPrivileges,
    TokenSessionReference,
    TokenSandBoxInert,
    TokenAuditPolicy,
    TokenOrigin
}
