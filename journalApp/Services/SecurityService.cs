using System.Security.Cryptography;
using System.Text;

namespace journalApp.Services
{
    public class SecurityService : ISecurityService
    {
        private const string PIN_KEY = "journal_app_pin";
        private const string USERNAME_KEY = "journal_app_username";
        private const string USERID_KEY = "journal_app_userid";
        private const string LOCK_STATE_KEY = "app_lock_state";
        private const string USERS_KEY = "journal_app_users";

        public async Task<bool> HasSecuritySetup()
        {
            try
            {
                var usersJson = Preferences.Default.Get(USERS_KEY, string.Empty);
                return !string.IsNullOrEmpty(usersJson);
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

                var userId = Guid.NewGuid().ToString();
                var hashedPin = HashPin(pin);

                var usersJson = Preferences.Default.Get(USERS_KEY, "{}");
                var users = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, UserData>>(usersJson) 
                    ?? new Dictionary<string, UserData>();

                var userKey = username.ToLower().Trim();
                
                if (users.ContainsKey(userKey))
                {
                    Console.WriteLine("Username already exists");
                    return false;
                }

                users[userKey] = new UserData 
                { 
                    UserId = userId, 
                    HashedPin = hashedPin,
                    Username = username
                };

                var updatedJson = System.Text.Json.JsonSerializer.Serialize(users);
                Preferences.Default.Set(USERS_KEY, updatedJson);

                Console.WriteLine($"User created: {username}, UserId: {userId}");
                Console.WriteLine($"Stored users: {updatedJson}");

                Preferences.Default.Set(USERNAME_KEY, username);
                Preferences.Default.Set(USERID_KEY, userId);
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

        public async Task<bool> LoginAsync(string username, string pin)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(pin))
                {
                    Console.WriteLine("Username or PIN is empty");
                    return false;
                }

                var usersJson = Preferences.Default.Get(USERS_KEY, "{}");
                Console.WriteLine($"Retrieved users JSON: {usersJson}");
                
                var users = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, UserData>>(usersJson) 
                    ?? new Dictionary<string, UserData>();

                var userKey = username.ToLower().Trim();
                Console.WriteLine($"Looking for user: {userKey}");
                Console.WriteLine($"Available users: {string.Join(", ", users.Keys)}");

                if (!users.ContainsKey(userKey))
                {
                    Console.WriteLine($"User not found: {userKey}");
                    return false;
                }

                var userData = users[userKey];
                var inputHash = HashPin(pin);

                Console.WriteLine($"Stored hash: {userData.HashedPin}");
                Console.WriteLine($"Input hash: {inputHash}");
                Console.WriteLine($"Hashes match: {userData.HashedPin == inputHash}");

                if (userData.HashedPin == inputHash)
                {
                    Console.WriteLine($"Login successful for user: {userData.Username}");
                    Preferences.Default.Set(USERNAME_KEY, userData.Username);
                    Preferences.Default.Set(USERID_KEY, userData.UserId);
                    return true;
                }

                Console.WriteLine("PIN mismatch");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LoginAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> ValidatePin(string pin)
        {
            try
            {
                var currentUsername = Preferences.Default.Get(USERNAME_KEY, string.Empty);
                if (string.IsNullOrEmpty(currentUsername))
                {
                    Console.WriteLine("No current username found");
                    return false;
                }

                var usersJson = Preferences.Default.Get(USERS_KEY, "{}");
                var users = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, UserData>>(usersJson) 
                    ?? new Dictionary<string, UserData>();

                var userKey = currentUsername.ToLower().Trim();
                
                if (!users.ContainsKey(userKey))
                {
                    Console.WriteLine($"Current user not found in database: {userKey}");
                    return false;
                }

                var userData = users[userKey];
                var inputHash = HashPin(pin);
                return userData.HashedPin == inputHash;
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

        public Task<string> GetUserId()
        {
            try
            {
                var userId = Preferences.Default.Get(USERID_KEY, string.Empty);
                return Task.FromResult(userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserId: {ex.Message}");
                return Task.FromResult(string.Empty);
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
                return true;
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
                return result;
            }
        }
        
        public async Task LogoutAsync()
        {
            try
            {
                Preferences.Default.Remove(USERNAME_KEY);
                Preferences.Default.Remove(USERID_KEY);
                Preferences.Default.Set(LOCK_STATE_KEY, "locked");
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during logout: {ex.Message}");
                throw;
            }
        }

        private class UserData
        {
            public string UserId { get; set; } = "";
            public string HashedPin { get; set; } = "";
            public string Username { get; set; } = "";
        }
    }
}