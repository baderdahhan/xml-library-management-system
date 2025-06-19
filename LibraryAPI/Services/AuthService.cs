using System.Security.Cryptography;
using System.Text;
using LibraryAPI.Models;

namespace LibraryAPI.Services
{
    public class AuthService
    {
        private readonly XmlService _xmlService;
        private readonly ILogger<AuthService> _logger;
        private const string FileName = "users.xml";

        public AuthService(XmlService xmlService, ILogger<AuthService> logger)
        {
            _xmlService = xmlService;
            _logger = logger;
            InitializeUsers();
        }

        private void InitializeUsers()
        {
            var users = _xmlService.LoadXml<Users>(FileName);
            if (users == null || !users.UserList.Any())
            {
                // Create default admin user if no users exist
                users = new Users
                {
                    UserList = new List<User>
                    {
                        new User
                        {
                            Id = 1,
                            Username = "admin",
                            PasswordHash = HashPassword("admin123"),
                            Role = "Admin"
                        },

                         new User
                        {
                            Id = 2,
                            Username = "bader",
                            PasswordHash = HashPassword("bader123"),
                            Role = "Admin"
                        }

                    }
                };
                _xmlService.SaveXml(users, FileName);
                _logger.LogInformation("Created default admin user");
            }
        }

        public User? ValidateUser(string username, string password)
        {
            try
            {
                var users = _xmlService.LoadXml<Users>(FileName);
                if (users?.UserList == null) return null;

                var user = users.UserList.FirstOrDefault(u => 
                    u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed: User {Username} not found", username);
                    return null;
                }

                if (!VerifyPassword(password, user.PasswordHash))
                {
                    _logger.LogWarning("Login attempt failed: Invalid password for user {Username}", username);
                    return null;
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user {Username}", username);
                return null;
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword.Equals(hash, StringComparison.Ordinal);
        }

        public User? CreateUser(string username, string password, string role = "User")
        {
            try
            {
                var users = _xmlService.LoadXml<Users>(FileName) ?? new Users { UserList = new List<User>() };

                if (users.UserList.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("User creation failed: Username {Username} already exists", username);
                    return null;
                }

                var newUser = new User
                {
                    Id = users.UserList.Count > 0 ? users.UserList.Max(u => u.Id) + 1 : 1,
                    Username = username,
                    PasswordHash = HashPassword(password),
                    Role = role
                };

                users.UserList.Add(newUser);
                _xmlService.SaveXml(users, FileName);
                _logger.LogInformation("Created new user: {Username}", username);

                return newUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Username}", username);
                return null;
            }
        }
    }
} 