using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using webprogbackend.Attributes;
using webprogbackend.Data;
using webprogbackend.Models;
using webprogbackend.Models.Enums;
using webprogbackend.Services;

namespace webprogbackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            IJwtService jwtService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    return BadRequest("Email already exists");
                }

                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    return BadRequest("Username already exists");
                }

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = UserRole.User // Default role
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create cart for new user
                var cart = new Cart
                {
                    UserId = user.Id
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation($"New user registered: {user.Email}");

                return Ok(new
                {
                    Success = true,
                    Message = "Registration successful",
                    Token = token,
                    User = new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new { Message = "An error occurred during registration" });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    return Unauthorized(new { Message = "Invalid email or password" });
                }

                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation($"User logged in: {user.Email}");

                return Ok(new
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    User = new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, new { Message = "An error occurred during login" });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult> GetCurrentUser()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                return Ok(new
                {
                    Success = true,
                    User = new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.Role,
                        user.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { Message = "An error occurred" });
            }
        }

        [Authorize]
        [HttpPost("refresh-token")]
        public async Task<ActionResult> RefreshToken()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                var token = _jwtService.GenerateToken(user);

                return Ok(new
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    Token = token,
                    User = new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { Message = "An error occurred" });
            }
        }

        [AuthorizeRoles(UserRole.Admin)]
        [HttpPost("change-role")]
        public async Task<ActionResult> ChangeUserRole(ChangeRoleModel model)
        {
            try
            {
                var user = await _context.Users.FindAsync(model.UserId);
                if (user == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                var oldRole = user.Role;
                user.Role = model.NewRole;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User role changed: {user.Email} from {oldRole} to {model.NewRole}");

                return Ok(new
                {
                    Success = true,
                    Message = "User role updated successfully",
                    User = new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing user role");
                return StatusCode(500, new { Message = "An error occurred" });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            try
            {
                // JWT tokenlar stateless olduðu için server-side logout yapamayýz
                // Client-side'da token'ý silmek yeterlidir
                _logger.LogInformation("User logged out");

                return Ok(new
                {
                    Success = true,
                    Message = "Logged out successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { Message = "An error occurred" });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    // Güvenlik sebebiyle kullanýcý bulunmasa bile baþarýlý mesajý döndürüyoruz
                    return Ok(new { Success = true, Message = "If the email exists, a reset link has been sent." });
                }

                // Gerçek uygulamada burada email gönderme servisi kullanýlýr
                _logger.LogInformation($"Password reset requested for: {model.Email}");

                return Ok(new { Success = true, Message = "If the email exists, a reset link has been sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password");
                return StatusCode(500, new { Message = "An error occurred" });
            }
        }

       
    }

    // DTO Models
    public class RegisterModel
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }
    }

    public class LoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class ChangeRoleModel
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public UserRole NewRole { get; set; }
    }

    public class ForgotPasswordModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ChangePasswordModel
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }
    }
}