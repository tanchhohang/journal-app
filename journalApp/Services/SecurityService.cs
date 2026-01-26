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
                try
                {
                    var pin = await SecureStorage.Default.GetAsync(PIN_KEY);
                    if (!string.IsNullOrEmpty(pin))
                    {
                        return true;
                    }
                }
                catch
                {
                    
                }

                var prefPin = Preferences.Default.Get(PIN_KEY, string.Empty);
                var hasPin = !string.IsNullOrEmpty(prefPin);
                return hasPin;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HasSecuritySetup: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SetupSecurity(string username, string pin)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(pin))
                {
                    Console.WriteLine("Username or PIN is empty");
                    return false;
                }

                if (pin.Length < 4)
                {
                    Console.WriteLine("PIN too short");
                    return false;
                }

                // hash the PIN
                var hashedPin = HashPin(pin);
                Console.WriteLine("=== PIN hashed successfully");

                bool useSecureStorage = false;
                try
                {
                    try
                    {
                        SecureStorage.Default.Remove(PIN_KEY);
                    }
                    catch
                    {
                    }

                    await SecureStorage.Default.SetAsync(PIN_KEY, hashedPin);
                    var test = await SecureStorage.Default.GetAsync(PIN_KEY);
                    if (!string.IsNullOrEmpty(test))
                    {
                        useSecureStorage = true;
                    }
                }
                catch (Exception ex)
                {
                   
                }
                
                if (!useSecureStorage)
                {
                    Preferences.Default.Remove(PIN_KEY);
                    Preferences.Default.Set(PIN_KEY, hashedPin);
                }

                // store username
                Preferences.Default.Set(USERNAME_KEY, username);

                // set initial state as unlocked
                Preferences.Default.Set(LOCK_STATE_KEY, "unlocked");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SetupSecurity: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> ValidatePin(string pin)
        {
            try
            {
                string storedHash = null;
                
                // try SecureStorage first
                try
                {
                    storedHash = await SecureStorage.Default.GetAsync(PIN_KEY);
                }
                catch
                {
                    // secureStorage not available
                }

                // fallback to Preferences if needed
                if (string.IsNullOrEmpty(storedHash))
                {
                    storedHash = Preferences.Default.Get(PIN_KEY, string.Empty);
                }

                if (string.IsNullOrEmpty(storedHash))
                {
                    Console.WriteLine("No stored PIN found");
                    return false;
                }

                var inputHash = HashPin(pin);
                var isValid = storedHash == inputHash;
                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ValidatePin: {ex.Message}");
                return false;
            }
        }

        public Task<string> GetUsername()
        {
            try
            {
                var username = Preferences.Default.Get(USERNAME_KEY, "User");
                return Task.FromResult(username);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUsername: {ex.Message}");
                return Task.FromResult("User");
            }
        }

        public async Task<bool> IsLocked()
        {
            try
            {
                var hasSetup = await HasSecuritySetup();
                if (!hasSetup)
                {
                    return false;
                }

                var lockState = Preferences.Default.Get(LOCK_STATE_KEY, "locked");
                var isLocked = lockState == "locked";
                return isLocked;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in IsLocked: {ex.Message}");
                return true; // default to locked for security
            }
        }

        public Task LockApp()
        {
            try
            {
                Preferences.Default.Set(LOCK_STATE_KEY, "locked");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LockApp: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task UnlockApp()
        {
            try
            {
                Preferences.Default.Set(LOCK_STATE_KEY, "unlocked");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UnlockApp: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        private string HashPin(string pin)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(pin);
                var hash = sha256.ComputeHash(bytes);
                var result = Convert.ToBase64String(hash);
                Console.WriteLine($"=== PIN hashed (length: {result.Length})");
                return result;
            }
        }

        // Method to reset security (useful for testing)
        public async Task ResetSecurity()
        {
            try
            {
                SecureStorage.Default.Remove(PIN_KEY);
                Preferences.Default.Remove(USERNAME_KEY);
                Preferences.Default.Remove(LOCK_STATE_KEY);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting security: {ex.Message}");
            }
        }
    }
}