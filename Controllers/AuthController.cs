using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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

        /// <summary>
        /// Kullanýcý kaydý - Varsayýlan olarak User rolü atanýr
        /// </summary>
        [HttpPost("Register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if user already exists
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    return BadRequest(new { message = "Email already registered" });
                }

                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    return BadRequest(new { message = "Username already taken" });
                }

                // Create new user with User role by default
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = UserRole.User, // Varsayýlan rol
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create cart for user
                var cart = new Cart
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation($"User registered successfully: {user.Email} with role: {user.Role}");

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role
                    },
                    Message = "Registration successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration");
                return StatusCode(500, new { message = "Registration failed" });
            }
        }

        /// <summary>
        /// Admin kaydý - Sadece mevcut adminler tarafýndan kullanýlabilir
        /// </summary>
        [HttpPost("RegisterAdmin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AuthResponse>> RegisterAdmin([FromBody] RegisterAdminModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if user already exists
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    return BadRequest(new { message = "Email already registered" });
                }

                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    return BadRequest(new { message = "Username already taken" });
                }

                // Create new admin user
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = model.Role, // Admin veya Moderator
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Admin için cart oluþturmaya gerek yok, ama istenirse eklenebilir
                if (model.CreateCart)
                {
                    var cart = new Cart
                    {
                        UserId = user.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var currentAdmin = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                _logger.LogInformation($"Admin user created: {user.Email} with role: {user.Role} by {currentAdmin}");

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = null, // Admin kaydýnda token vermiyoruz
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role
                    },
                    Message = $"{user.Role} user created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during admin registration");
                return StatusCode(500, new { message = "Admin registration failed" });
            }
        }

        /// <summary>
        /// Kullanýcý giriþi - Rol bilgisi token'da döner
        /// </summary>
        [HttpPost("Login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    return BadRequest(new { message = "Invalid email or password" });
                }

                // Generate JWT token with role information
                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation($"User logged in: {user.Email} with role: {user.Role}");

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role
                    },
                    Message = "Login successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user login");
                return StatusCode(500, new { message = "Login failed" });
            }
        }

        /// <summary>
        /// Kullanýcý rolünü deðiþtir - Sadece Admin yapabilir
        /// </summary>
        [HttpPut("ChangeUserRole/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ChangeUserRole(int userId, [FromBody] ChangeRoleModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var oldRole = user.Role;
                user.Role = model.NewRole;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var adminUser = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown Admin";
                _logger.LogInformation($"User role changed: {user.Email} from {oldRole} to {model.NewRole} by {adminUser}");

                return Ok(new
                {
                    message = "User role updated successfully",
                    userId = user.Id,
                    email = user.Email,
                    oldRole = oldRole.ToString(),
                    newRole = model.NewRole.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during role change");
                return StatusCode(500, new { message = "Role change failed" });
            }
        }

        /// <summary>
        /// Mevcut kullanýcýnýn rol bilgilerini getir
        /// </summary>
        [HttpGet("MyRole")]
        [Authorize]
        public async Task<ActionResult<UserRoleInfo>> GetMyRole()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(new UserRoleInfo
                {
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    RoleName = user.Role.ToString(),
                    Permissions = GetRolePermissions(user.Role),
                    IsAdmin = user.Role == UserRole.Admin,
                    IsModerator = user.Role == UserRole.Moderator,
                    IsUser = user.Role == UserRole.User
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user role");
                return StatusCode(500, new { message = "Failed to get role information" });
            }
        }

        /// <summary>
        /// Tüm kullanýcýlarýn rol bilgilerini listele - Sadece Admin
        /// </summary>
        [HttpGet("UserRoles")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserRoleInfo>>> GetAllUserRoles(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] UserRole? roleFilter = null)
        {
            try
            {
                var query = _context.Users.AsQueryable();

                if (roleFilter.HasValue)
                {
                    query = query.Where(u => u.Role == roleFilter.Value);
                }

                var totalCount = await query.CountAsync();
                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserRoleInfo
                    {
                        Username = u.Username,
                        Email = u.Email,
                        Role = u.Role,
                        RoleName = u.Role.ToString(),
                        IsAdmin = u.Role == UserRole.Admin,
                        IsModerator = u.Role == UserRole.Moderator,
                        IsUser = u.Role == UserRole.User,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt
                    })
                    .ToListAsync();

                // Add permissions to each user
                users.ForEach(u => u.Permissions = GetRolePermissions(u.Role));

                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Current-Page", page.ToString());

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user roles");
                return StatusCode(500, new { message = "Failed to get user roles" });
            }
        }

        // Diðer mevcut metodlar (ChangePassword, GetCurrentUser, Logout, ValidateToken)
        [HttpPost("ChangePassword")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.Password))
                {
                    return BadRequest(new { message = "Current password is incorrect" });
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Password changed successfully for user: {user.Email}");

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during password change");
                return StatusCode(500, new { message = "Password change failed" });
            }
        }

        [HttpGet("Me")]
        [Authorize]
        public async Task<ActionResult<UserInfo>> GetCurrentUser()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting current user");
                return StatusCode(500, new { message = "Failed to get user information" });
            }
        }

        [HttpPost("Logout")]
        [Authorize]
        public ActionResult Logout()
        {
            return Ok(new { message = "Logout successful" });
        }

        [HttpPost("ValidateToken")]
        public ActionResult ValidateToken([FromBody] ValidateTokenModel model)
        {
            try
            {
                var isValid = _jwtService.IsTokenValid(model.Token);
                return Ok(new { isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token validation");
                return Ok(new { isValid = false });
            }
        }

        #region Private Methods

        private List<string> GetRolePermissions(UserRole role)
        {
            return role switch
            {
                UserRole.Admin => new List<string>
                {
                    "user.manage",
                    "product.manage",
                    "order.manage",
                    "dashboard.admin",
                    "role.change",
                    "system.admin"
                },
                UserRole.Moderator => new List<string>
                {
                    "product.manage",
                    "order.view",
                    "user.view",
                    "dashboard.moderator"
                },
                UserRole.User => new List<string>
                {
                    "product.view",
                    "cart.manage",
                    "order.own",
                    "profile.manage"
                },
                _ => new List<string>()
            };
        }

        #endregion
    }

    #region DTO Models

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

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }

    public class RegisterAdminModel : RegisterModel
    {
        [Required]
        public UserRole Role { get; set; } = UserRole.Admin;

        public bool CreateCart { get; set; } = false;
    }

    public class LoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class ChangePasswordModel
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; }
    }

    public class ChangeRoleModel
    {
        [Required]
        public UserRole NewRole { get; set; }

        public string? Reason { get; set; }
    }

    public class ValidateTokenModel
    {
        [Required]
        public string Token { get; set; }
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public UserInfo User { get; set; }
        public string Message { get; set; }
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
    }

    public class UserRoleInfo : UserInfo
    {
        public string RoleName { get; set; }
        public List<string> Permissions { get; set; } = new();
        public bool IsAdmin { get; set; }
        public bool IsModerator { get; set; }
        public bool IsUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    #endregion
}