using LovePath.Interface;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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

    public class ImpersonateUser : IDisposable
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

            _userImpersonationObj = (IUserImpersonation)GetInstance(impersonationType);
        }

        public object GetInstance(ImpersonationType chosenType)
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
        }

        public bool RunImpersonatedValidUser(Action action)
        {
            // Check the identity.
            Debug.WriteLine($"Before impersonation: {WindowsIdentity.GetCurrent().Name}");

            if (_userImpersonationObj.ImpersonateValidUser())
            {
                action();

                // Check the identity.
                Debug.WriteLine($"After impersonation: {WindowsIdentity.GetCurrent().Name}");
            }
            else
                return false;

            return true;
        }

        public bool RunImpersonated(Action action)
        {
            return _userImpersonationObj.RunImpersonated(action);
        }

        public void Dispose()
        {
            _userImpersonationObj.Dispose();
        }
    }
}