namespace journalApp.Services
{
    public interface ISecurityService
    {
        Task<bool> HasSecuritySetup();
        Task<bool> SetupSecurity(string username, string pin);
        Task<bool> ValidatePin(string pin);
        Task<bool> LoginAsync(string username, string pin);
        Task<string> GetUsername();
        Task<string> GetUserId();
        Task<bool> IsLocked();
        Task LockApp();
        Task UnlockApp();
        Task LogoutAsync();
    }
}