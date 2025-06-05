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
        /// Kullan�c� kay�t i�lemi
        /// </summary>
        [HttpPost("Register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Ge�ersiz veri giri�i",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                // Email kontrol�
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (existingUserByEmail != null)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Bu email adresi zaten kullan�l�yor"
                    });
                }

                // Username kontrol�
                var existingUserByUsername = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

                if (existingUserByUsername != null)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Bu kullan�c� ad� zaten kullan�l�yor"
                    });
                }

                // �ifre kontrol�
                if (request.Password != request.ConfirmPassword)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "�ifreler e�le�miyor"
                    });
                }

                // Yeni kullan�c� olu�tur
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = UserRole.User, // Varsay�lan olarak User rol�
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Kullan�c� i�in sepet olu�tur
                var cart = new Cart
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                // JWT token olu�tur
                var token = _jwtService.GenerateToken(user);

                // Ho� geldin emaili g�nder (arka planda)
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Welcome email could not be sent to {Email}", user.Email);
                }

                _logger.LogInformation("New user registered: {Email}", user.Email);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Kay�t ba�ar�yla tamamland�",
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "Kay�t s�ras�nda bir hata olu�tu. L�tfen tekrar deneyin."
                });
            }
        }

        [HttpPost("Register-Admin")]
        public async Task<ActionResult<AuthResponse>> RegisterAdmin([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Ge�ersiz veri giri�i",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                // Email kontrol�
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (existingUserByEmail != null)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Bu email adresi zaten kullan�l�yor"
                    });
                }

                // Username kontrol�
                var existingUserByUsername = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

                if (existingUserByUsername != null)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Bu kullan�c� ad� zaten kullan�l�yor"
                    });
                }

                // �ifre kontrol�
                if (request.Password != request.ConfirmPassword)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "�ifreler e�le�miyor"
                    });
                }

                // Yeni kullan�c� olu�tur
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Kullan�c� i�in sepet olu�tur
                var cart = new Cart
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                // JWT token olu�tur
                var token = _jwtService.GenerateToken(user);

                // Ho� geldin emaili g�nder (arka planda)
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Welcome email could not be sent to {Email}", user.Email);
                }

                _logger.LogInformation("New user registered: {Email}", user.Email);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Kay�t ba�ar�yla tamamland�",
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "Kay�t s�ras�nda bir hata olu�tu. L�tfen tekrar deneyin."
                });
            }
        }



        /// <summary>
        /// Kullan�c� giri� i�lemi
        /// </summary>
        [HttpPost("Login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Ge�ersiz veri giri�i",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                // Kullan�c�y� email ile bul
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (user == null)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Email veya �ifre hatal�"
                    });
                }

                // �ifre kontrol�
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Email veya �ifre hatal�"
                    });
                }

                // JWT token olu�tur
                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation("User logged in: {Email}", user.Email);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Giri� ba�ar�l�",
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "Giri� s�ras�nda bir hata olu�tu. L�tfen tekrar deneyin."
                });
            }
        }

        /// <summary>
        /// Mevcut kullan�c� bilgilerini getir
        /// </summary>
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
                    return NotFound(new { message = "Kullan�c� bulunamad�" });
                }

                return Ok(new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "Kullan�c� bilgileri al�n�rken hata olu�tu" });
            }
        }

        /// <summary>
        /// Kullan�c� rol�n� getir
        /// </summary>
        [HttpGet("MyRole")]
        [Authorize]
        public ActionResult<object> GetMyRole()
        {
            try
            {
                var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ??
                               User.FindFirst("Role")?.Value;

                return Ok(new
                {
                    Role = roleClaim,
                    IsAdmin = roleClaim == UserRole.Admin.ToString(),
                    IsUser = roleClaim == UserRole.User.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user role");
                return StatusCode(500, new { message = "Rol bilgisi al�n�rken hata olu�tu" });
            }
        }

        /// <summary>
        /// �ifre de�i�tirme
        /// </summary>
        [HttpPost("ChangePassword")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
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
                    return NotFound(new { message = "Kullan�c� bulunamad�" });
                }

                // Mevcut �ifre kontrol�
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
                {
                    return BadRequest(new { message = "Mevcut �ifre hatal�" });
                }

                // Yeni �ifre kontrol�
                if (request.NewPassword != request.ConfirmNewPassword)
                {
                    return BadRequest(new { message = "Yeni �ifreler e�le�miyor" });
                }

                // �ifreyi g�ncelle
                user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed for user: {Email}", user.Email);

                return Ok(new { message = "�ifre ba�ar�yla de�i�tirildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { message = "�ifre de�i�tirme s�ras�nda hata olu�tu" });
            }
        }

        /// <summary>
        /// Token do�rulama
        /// </summary>
        [HttpPost("ValidateToken")]
        public ActionResult ValidateToken([FromBody] TokenValidationRequest request)
        {
            try
            {
                var isValid = _jwtService.IsTokenValid(request.Token);

                return Ok(new
                {
                    IsValid = isValid,
                    Message = isValid ? "Token ge�erli" : "Token ge�ersiz"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return Ok(new
                {
                    IsValid = false,
                    Message = "Token do�rulama s�ras�nda hata olu�tu"
                });
            }
        }

        /// <summary>
        /// ��k�� i�lemi (client-side token temizleme)
        /// </summary>
        [HttpPost("Logout")]
        [Authorize]
        public ActionResult Logout()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                _logger.LogInformation("User logged out: {Email}", userEmail);

                return Ok(new { message = "��k�� ba�ar�l�" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return Ok(new { message = "��k�� i�lemi tamamland�" });
            }
        }

        /// <summary>
        /// Hesap silme
        /// </summary>
        [HttpDelete("DeleteAccount")]
        [Authorize]
        public async Task<ActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Kullan�c� bulunamad�" });
                }

                // �ifre kontrol�
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                {
                    return BadRequest(new { message = "�ifre hatal�" });
                }

                // �li�kili verileri sil (cascade delete �al��acak)
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User account deleted: {Email}", user.Email);

                return Ok(new { message = "Hesap ba�ar�yla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account");
                return StatusCode(500, new { message = "Hesap silme s�ras�nda hata olu�tu" });
            }
        }
    }

    #region Request/Response Models

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Kullan�c� ad� gereklidir")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullan�c� ad� 3-50 karakter aras�nda olmal�d�r")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email gereklidir")]
        [EmailAddress(ErrorMessage = "Ge�erli bir email adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "�ifre gereklidir")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "�ifre en az 6 karakter olmal�d�r")]
        public string Password { get; set; }

        [Required(ErrorMessage = "�ifre tekrar� gereklidir")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Email gereklidir")]
        [EmailAddress(ErrorMessage = "Ge�erli bir email adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "�ifre gereklidir")]
        public string Password { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Mevcut �ifre gereklidir")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Yeni �ifre gereklidir")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "�ifre en az 6 karakter olmal�d�r")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Yeni �ifre tekrar� gereklidir")]
        public string ConfirmNewPassword { get; set; }
    }

    public class TokenValidationRequest
    {
        [Required]
        public string Token { get; set; }
    }

    public class DeleteAccountRequest
    {
        [Required(ErrorMessage = "�ifre gereklidir")]
        public string Password { get; set; }
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public UserInfo User { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    #endregion
}