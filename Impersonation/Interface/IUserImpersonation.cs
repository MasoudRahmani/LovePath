using System;

namespace LovePath.Interface
{
    public interface IUserImpersonation
    {
        void Init(string username, string domain, System.Security.SecureString password);


        bool ImpersonateValidUser();
        
        bool RunImpersonated(Action action);
        void Dispose();
    }
}
