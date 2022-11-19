using System;

namespace LovePath.Interface
{
    public interface IUserImpersonation
    {
        bool ImpersonateValidUser();
        
        bool RunImpersonated(Action action);



        void Dispose();
    }
}
