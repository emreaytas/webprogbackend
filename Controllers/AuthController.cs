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
        /// Kullan�c� Kay�t
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

                // E-posta kontrol�
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest(new { message = "Bu e-posta adresi zaten kullan�l�yor" });
                }

                // Kullan�c� ad� kontrol�
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    return BadRequest(new { message = "Bu kullan�c� ad� zaten kullan�l�yor" });
                }

                // Yeni kullan�c� olu�tur
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = UserRole.User, // Normal kullan�c� olarak kaydet
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // JWT token olu�tur
                var token = _jwtService.GenerateToken(user);

                // Ho� geldin emaili g�nder (opsiyonel)
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Ho� geldin emaili g�nderilemedi: {Email}", user.Email);
                }

                _logger.LogInformation("Yeni kullan�c� kayd�: {Email}", user.Email);

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
                    Message = "Kay�t ba�ar�l�"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullan�c� kayd� s�ras�nda hata olu�tu");
                return StatusCode(500, new { message = "Kay�t s�ras�nda bir hata olu�tu" });
            }
        }

        /// <summary>
        /// Kullan�c� Giri�
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

                // Kullan�c�y� bul
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email || u.Username == request.Email);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                {
                    return BadRequest(new { message = "E-posta/kullan�c� ad� veya �ifre hatal�" });
                }

                // JWT token olu�tur
                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation("Kullan�c� giri�i: {Email}", user.Email);

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
                    Message = "Giri� ba�ar�l�"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullan�c� giri�i s�ras�nda hata olu�tu");
                return StatusCode(500, new { message = "Giri� s�ras�nda bir hata olu�tu" });
            }
        }

        /// <summary>
        /// Admin Kullan�c� Kay�t (Sadece Admin)
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

                // E-posta kontrol�
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest(new { message = "Bu e-posta adresi zaten kullan�l�yor" });
                }

                // Kullan�c� ad� kontrol�
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    return BadRequest(new { message = "Bu kullan�c� ad� zaten kullan�l�yor" });
                }

                // Yeni kullan�c� olu�tur
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = request.Role, // Admin istedi�i rol� verebilir
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // JWT token olu�tur
                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation("Admin taraf�ndan yeni kullan�c� olu�turuldu: {Email}, Role: {Role}", user.Email, user.Role);

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
                    Message = "Admin kay�t ba�ar�l�"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin kullan�c� kayd� s�ras�nda hata olu�tu");
                return StatusCode(500, new { message = "Kay�t s�ras�nda bir hata olu�tu" });
            }
        }

        /// <summary>
        /// Admin Giri� (Sadece Admin Rol�)
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

                // Admin kullan�c�s�n� bul
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => (u.Email == request.Email || u.Username == request.Email)
                                            && u.Role == UserRole.Admin);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                {
                    return BadRequest(new { message = "Admin giri�i ba�ar�s�z - bilgiler hatal� veya yetkiniz yok" });
                }

                // JWT token olu�tur
                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation("Admin giri�i: {Email}", user.Email);

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
                    Message = "Admin giri� ba�ar�l�"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin giri�i s�ras�nda hata olu�tu");
                return StatusCode(500, new { message = "Giri� s�ras�nda bir hata olu�tu" });
            }
        }

        /// <summary>
        /// �ifre De�i�tir
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
                    return NotFound(new { message = "Kullan�c� bulunamad�" });
                }

                // Mevcut �ifreyi kontrol et
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
                {
                    return BadRequest(new { message = "Mevcut �ifre hatal�" });
                }

                // Yeni �ifreyi kaydet
                user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("�ifre de�i�tirildi: {Email}", user.Email);

                return Ok(new { message = "�ifre ba�ar�yla de�i�tirildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ifre de�i�tirme s�ras�nda hata olu�tu");
                return StatusCode(500, new { message = "�ifre de�i�tirme s�ras�nda bir hata olu�tu" });
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
                    return NotFound(new { message = "Kullan�c� bulunamad�" });
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
                _logger.LogError(ex, "Profil getirme s�ras�nda hata olu�tu");
                return StatusCode(500, new { message = "Profil bilgileri al�namad�" });
            }
        }

        /// <summary>
        /// Token Ge�erlili�ini Kontrol Et
        /// </summary>
        [HttpPost("validate-token")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            return Ok(new { message = "Token ge�erli", isValid = true });
        }

        /// <summary>
        /// ��k�� Yap (Token client-side'da silinir)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            _logger.LogInformation("Kullan�c� ��k��� yap�ld�");
            return Ok(new { message = "Ba�ar�yla ��k�� yap�ld�" });
        }
    }

    #region DTO Models

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Kullan�c� ad� zorunludur")]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur")]
        [EmailAddress(ErrorMessage = "Ge�erli bir e-posta adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "�ifre zorunludur")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "�ifre en az 6 karakter olmal�d�r")]
        public string Password { get; set; }
    }

    public class AdminRegisterRequest
    {
        [Required(ErrorMessage = "Kullan�c� ad� zorunludur")]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur")]
        [EmailAddress(ErrorMessage = "Ge�erli bir e-posta adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "�ifre zorunludur")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "�ifre en az 6 karakter olmal�d�r")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Rol zorunludur")]
        public UserRole Role { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "E-posta veya kullan�c� ad� zorunludur")]
        public string Email { get; set; }

        [Required(ErrorMessage = "�ifre zorunludur")]
        public string Password { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Mevcut �ifre zorunludur")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Yeni �ifre zorunludur")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "�ifre en az 6 karakter olmal�d�r")]
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