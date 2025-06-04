using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
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
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            IJwtService jwtService,
            IEmailService emailService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Kullanýcý Kayýt
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // E-posta kontrolü
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest(new { message = "Bu e-posta adresi zaten kullanýlýyor" });
                }

                // Kullanýcý adý kontrolü
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    return BadRequest(new { message = "Bu kullanýcý adý zaten kullanýlýyor" });
                }

                // Yeni kullanýcý oluþtur
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = UserRole.User, // Normal kullanýcý olarak kaydet
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // JWT token oluþtur
                var token = _jwtService.GenerateToken(user);

                // Hoþ geldin emaili gönder (opsiyonel)
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Hoþ geldin emaili gönderilemedi: {Email}", user.Email);
                }

                _logger.LogInformation("Yeni kullanýcý kaydý: {Email}", user.Email);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role,
                        CreatedAt = user.CreatedAt
                    },
                    Message = "Kayýt baþarýlý"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanýcý kaydý sýrasýnda hata oluþtu");
                return StatusCode(500, new { message = "Kayýt sýrasýnda bir hata oluþtu" });
            }
        }

        /// <summary>
        /// Kullanýcý Giriþ
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Kullanýcýyý bul
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email || u.Username == request.Email);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                {
                    return BadRequest(new { message = "E-posta/kullanýcý adý veya þifre hatalý" });
                }

                // JWT token oluþtur
                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation("Kullanýcý giriþi: {Email}", user.Email);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role,
                        CreatedAt = user.CreatedAt
                    },
                    Message = "Giriþ baþarýlý"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanýcý giriþi sýrasýnda hata oluþtu");
                return StatusCode(500, new { message = "Giriþ sýrasýnda bir hata oluþtu" });
            }
        }

        /// <summary>
        /// Admin Kullanýcý Kayýt (Sadece Admin)
        /// </summary>
        [HttpPost("admin/register")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AuthResponse>> AdminRegister([FromBody] AdminRegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // E-posta kontrolü
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest(new { message = "Bu e-posta adresi zaten kullanýlýyor" });
                }

                // Kullanýcý adý kontrolü
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    return BadRequest(new { message = "Bu kullanýcý adý zaten kullanýlýyor" });
                }

                // Yeni kullanýcý oluþtur
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = request.Role, // Admin istediði rolü verebilir
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // JWT token oluþtur
                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation("Admin tarafýndan yeni kullanýcý oluþturuldu: {Email}, Role: {Role}", user.Email, user.Role);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role,
                        CreatedAt = user.CreatedAt
                    },
                    Message = "Admin kayýt baþarýlý"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin kullanýcý kaydý sýrasýnda hata oluþtu");
                return StatusCode(500, new { message = "Kayýt sýrasýnda bir hata oluþtu" });
            }
        }

        /// <summary>
        /// Admin Giriþ (Sadece Admin Rolü)
        /// </summary>
        [HttpPost("admin/login")]
        public async Task<ActionResult<AuthResponse>> AdminLogin([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Admin kullanýcýsýný bul
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => (u.Email == request.Email || u.Username == request.Email)
                                            && u.Role == UserRole.Admin);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                {
                    return BadRequest(new { message = "Admin giriþi baþarýsýz - bilgiler hatalý veya yetkiniz yok" });
                }

                // JWT token oluþtur
                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation("Admin giriþi: {Email}", user.Email);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role,
                        CreatedAt = user.CreatedAt
                    },
                    Message = "Admin giriþ baþarýlý"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin giriþi sýrasýnda hata oluþtu");
                return StatusCode(500, new { message = "Giriþ sýrasýnda bir hata oluþtu" });
            }
        }

        /// <summary>
        /// Þifre Deðiþtir
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Kullanýcý bulunamadý" });
                }

                // Mevcut þifreyi kontrol et
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
                {
                    return BadRequest(new { message = "Mevcut þifre hatalý" });
                }

                // Yeni þifreyi kaydet
                user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Þifre deðiþtirildi: {Email}", user.Email);

                return Ok(new { message = "Þifre baþarýyla deðiþtirildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Þifre deðiþtirme sýrasýnda hata oluþtu");
                return StatusCode(500, new { message = "Þifre deðiþtirme sýrasýnda bir hata oluþtu" });
            }
        }

        /// <summary>
        /// Profil Bilgilerini Getir
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserInfo>> GetProfile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users
                    .Include(u => u.Orders)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new { message = "Kullanýcý bulunamadý" });
                }

                return Ok(new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt,
                    OrderCount = user.Orders.Count(),
                    TotalSpent = user.Orders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profil getirme sýrasýnda hata oluþtu");
                return StatusCode(500, new { message = "Profil bilgileri alýnamadý" });
            }
        }

        /// <summary>
        /// Token Geçerliliðini Kontrol Et
        /// </summary>
        [HttpPost("validate-token")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            return Ok(new { message = "Token geçerli", isValid = true });
        }

        /// <summary>
        /// Çýkýþ Yap (Token client-side'da silinir)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            _logger.LogInformation("Kullanýcý çýkýþý yapýldý");
            return Ok(new { message = "Baþarýyla çýkýþ yapýldý" });
        }
    }

    #region DTO Models

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Kullanýcý adý zorunludur")]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Þifre zorunludur")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Þifre en az 6 karakter olmalýdýr")]
        public string Password { get; set; }
    }

    public class AdminRegisterRequest
    {
        [Required(ErrorMessage = "Kullanýcý adý zorunludur")]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Þifre zorunludur")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Þifre en az 6 karakter olmalýdýr")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Rol zorunludur")]
        public UserRole Role { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "E-posta veya kullanýcý adý zorunludur")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Þifre zorunludur")]
        public string Password { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Mevcut þifre zorunludur")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Yeni þifre zorunludur")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Þifre en az 6 karakter olmalýdýr")]
        public string NewPassword { get; set; }
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
        public DateTime CreatedAt { get; set; }
        public int OrderCount { get; set; } = 0;
        public decimal TotalSpent { get; set; } = 0;
    }

    #endregion
}