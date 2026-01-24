using System.Security.Cryptography;
using System.Text;

namespace journalApp.Services
{
    public class SecurityService : ISecurityService
    {
        private const string PIN_KEY = "journal_app_pin";
        private const string USERNAME_KEY = "journal_app_username";
        private const string LOCK_STATE_KEY = "app_lock_state";

        public async Task<bool> HasSecuritySetup()
        {
            try
            {
                var pin = await SecureStorage.Default.GetAsync(PIN_KEY);
                return !string.IsNullOrEmpty(pin);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SetupSecurity(string username, string pin)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(pin))
                    return false;

                if (pin.Length < 4)
                    return false;

                var hashedPin = HashPin(pin);
                await SecureStorage.Default.SetAsync(PIN_KEY, hashedPin);
                Preferences.Default.Set(USERNAME_KEY, username);
                Preferences.Default.Set(LOCK_STATE_KEY, "unlocked");

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidatePin(string pin)
        {
            try
            {
                var storedHash = await SecureStorage.Default.GetAsync(PIN_KEY);
                if (string.IsNullOrEmpty(storedHash))
                    return false;

                var inputHash = HashPin(pin);
                return storedHash == inputHash;
            }
            catch
            {
                return false;
            }
        }

        public Task<string> GetUsername()
        {
            try
            {
                return Task.FromResult(Preferences.Default.Get(USERNAME_KEY, "User"));
            }
            catch
            {
                return Task.FromResult("User");
            }
        }

        public async Task<bool> IsLocked()
        {
            try
            {
                var hasSetup = await HasSecuritySetup();
                if (!hasSetup)
                    return false;

                var lockState = Preferences.Default.Get(LOCK_STATE_KEY, "locked");
                return lockState == "locked";
            }
            catch
            {
                return true;
            }
        }

        public Task LockApp()
        {
            try
            {
                Preferences.Default.Set(LOCK_STATE_KEY, "locked");
            }
            catch
            {
            }
            return Task.CompletedTask;
        }

        public Task UnlockApp()
        {
            try
            {
                Preferences.Default.Set(LOCK_STATE_KEY, "unlocked");
            }
            catch
            {
                // Handle error silently
            }
            return Task.CompletedTask;
        }

        private string HashPin(string pin)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(pin);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}