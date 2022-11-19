namespace LovePath.Interface
{
    public interface IUserImpersonation
    {
        bool ImpersonateValidUser(string account, string domain, string password);
        void Dispose();
    }
}
