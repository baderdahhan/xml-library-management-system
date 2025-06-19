using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LibraryAPI.Models;
using LibraryAPI.Services;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly AuthService _authService;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger, AuthService authService)
        {
            _configuration = configuration;
            _logger = logger;
            _authService = authService;
        }
        
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            try
            {
                if (model == null)
                {
                    _logger.LogWarning("Login attempt with null model");
                    return BadRequest(new { message = "Invalid request body" });
                }

                if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
                {
                    _logger.LogWarning("Login attempt with empty username or password");
                    return BadRequest(new { message = "Username and password are required" });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    _logger.LogWarning("Login attempt with invalid model state: {Errors}", string.Join(", ", errors));
                    return BadRequest(new { message = "Invalid username or password format", errors });
                }

                try
                {
                    var user = _authService.ValidateUser(model.Username, model.Password);
                    if (user == null)
                    {
                        _logger.LogWarning("Failed login attempt for user: {Username}", model.Username);
                        return Unauthorized(new { message = "Invalid username or password" });
                    }

                    var token = GenerateJwtToken(user);
                    _logger.LogInformation("User {Username} logged in successfully", model.Username);

                    return Ok(new
                    {
                        token,
                        username = user.Username,
                        role = user.Role
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating user credentials for user: {Username}", model.Username);
                    return StatusCode(500, new { message = "An error occurred while validating credentials" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for user: {Username}", model?.Username);
                return StatusCode(500, new { message = "An unexpected error occurred during login" });
            }
        }

        [HttpGet("verify")]
        public IActionResult VerifyToken()
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized();
                }

                var principal = ValidateToken(token);
                if (principal == null)
                {
                    return Unauthorized();
                }

                var username = principal.FindFirst(ClaimTypes.Name)?.Value;
                var role = principal.FindFirst(ClaimTypes.Role)?.Value;

                return Ok(new { username, role });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying token");
                return StatusCode(500, "An error occurred while verifying the token");
            }
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }
    }

    public class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
} 