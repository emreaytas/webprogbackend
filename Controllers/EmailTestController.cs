using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webprogbackend.Services;

namespace webprogbackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailTestController> _logger;

        public EmailTestController(IEmailService emailService, ILogger<EmailTestController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        // Test email gönder
        [HttpPost("test")]
        public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
        {
            try
            {
                await _emailService.SendTestEmailAsync(request.Email);

                return Ok(new
                {
                    success = true,
                    message = $"Test email'i {request.Email} adresine gönderildi"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Test email gönderilirken hata: {request.Email}");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Sipariş onay email'i test et - DÜZELTME: Otomatik sipariş verisi oluştur
        [HttpPost("test-order-confirmation")]
        public async Task<IActionResult> TestOrderConfirmation([FromBody] TestOrderEmailRequest request)
        {
            try
            {
                var orderNumber = "ORD-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999);
                var totalAmount = 1299.99m; // Test tutarı

                await _emailService.SendOrderConfirmationAsync(
                    request.Email,
                    orderNumber,
                    totalAmount
                );

                return Ok(new
                {
                    success = true,
                    message = $"Sipariş onay email'i {request.Email} adresine gönderildi",
                    orderNumber = orderNumber,
                    totalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Sipariş onay email'i gönderilirken hata: {request.Email}");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Hoş geldin email'i test et
        [HttpPost("test-welcome")]
        public async Task<IActionResult> TestWelcomeEmail([FromBody] TestWelcomeEmailRequest request)
        {
            try
            {
                await _emailService.SendWelcomeEmailAsync(request.Email, request.Username);

                return Ok(new
                {
                    success = true,
                    message = $"Hoş geldin email'i {request.Email} adresine gönderildi"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Hoş geldin email'i gönderilirken hata: {request.Email}");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Şifre sıfırlama email'i test et
        [HttpPost("test-password-reset")]
        public async Task<IActionResult> TestPasswordReset([FromBody] TestEmailRequest request)
        {
            try
            {
                var resetLink = "https://localhost:7062/reset-password?token=test-token-123";
                await _emailService.SendPasswordResetAsync(request.Email, resetLink);

                return Ok(new
                {
                    success = true,
                    message = $"Şifre sıfırlama email'i {request.Email} adresine gönderildi"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Şifre sıfırlama email'i gönderilirken hata: {request.Email}");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Admin bildirimi test et
        [HttpPost("test-admin-notification")]
        public async Task<IActionResult> TestAdminNotification([FromBody] AdminNotificationRequest request)
        {
            try
            {
                _logger.LogInformation("Admin bildirimi gönderiliyor...");
                _logger.LogInformation($"Subject: {request.Subject}");
                _logger.LogInformation($"Message Length: {request.Message?.Length ?? 0}");

                await _emailService.SendAdminNotificationAsync(request.Subject, request.Message);

                return Ok(new
                {
                    success = true,
                    message = "Admin bildirimi başarıyla gönderildi",
                    timestamp = DateTime.Now,
                    details = new
                    {
                        subject = request.Subject,
                        messageLength = request.Message?.Length ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin bildirimi gönderilirken hata");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }

        // Yeni sipariş bildirimi test et
        [HttpPost("test-new-order")]
        public async Task<IActionResult> TestNewOrderNotification([FromBody] NewOrderNotificationRequest request)
        {
            try
            {
                await _emailService.SendNewOrderNotificationAsync(request.OrderDetails);

                return Ok(new
                {
                    success = true,
                    message = "Yeni sipariş bildirimi gönderildi"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yeni sipariş bildirimi gönderilirken hata");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Email ayarlarını kontrol et
        [HttpGet("check-settings")]
        public IActionResult CheckEmailSettings()
        {
            var config = HttpContext.RequestServices.GetService<IConfiguration>();

            return Ok(new
            {
                smtpConfigured = !string.IsNullOrEmpty(config["EmailSettings:SmtpServer"]),
                fromEmailConfigured = !string.IsNullOrEmpty(config["EmailSettings:FromEmail"]),
                passwordConfigured = !string.IsNullOrEmpty(config["EmailSettings:Password"]),
                recipientConfigured = !string.IsNullOrEmpty(config["EmailSettings:DefaultRecipientEmail"]),
                settings = new
                {
                    smtpServer = config["EmailSettings:SmtpServer"],
                    port = config["EmailSettings:Port"],
                    fromEmail = config["EmailSettings:FromEmail"],
                    fromName = config["EmailSettings:FromName"],
                    defaultRecipient = config["EmailSettings:DefaultRecipientEmail"]
                },
                message = "Email ayarları kontrol edildi",
                timestamp = DateTime.Now
            });
        }

        // Email bağlantısını test et
        [HttpPost("test-connection")]
        public async Task<IActionResult> TestEmailConnection()
        {
            try
            {
                var emailService = _emailService as EmailService;
                if (emailService != null)
                {
                    var isWorking = await emailService.TestEmailConfigurationAsync();

                    if (isWorking)
                    {
                        return Ok(new
                        {
                            success = true,
                            message = "Email bağlantısı başarılı",
                            timestamp = DateTime.Now
                        });
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Email bağlantısı başarısız",
                            timestamp = DateTime.Now
                        });
                    }
                }

                return BadRequest(new
                {
                    success = false,
                    message = "EmailService bulunamadı"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email bağlantısı test edilirken hata");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Gmail SMTP özel testi - DÜZELTME: Daha detaylı hata kontrolü
        [HttpPost("test-gmail")]
        public async Task<IActionResult> TestGmailSmtp()
        {
            try
            {
                var config = HttpContext.RequestServices.GetService<IConfiguration>();
                var testEmail = config["EmailSettings:DefaultRecipientEmail"] ?? "emreaytascmp@gmail.com";

                _logger.LogInformation($"Gmail SMTP testi başlatılıyor - Hedef: {testEmail}");

                await _emailService.SendTestEmailAsync(testEmail);

                _logger.LogInformation("Gmail SMTP testi başarılı");

                return Ok(new
                {
                    success = true,
                    message = $"Gmail SMTP testi başarılı - {testEmail} adresine gönderildi",
                    info = new
                    {
                        smtp = "smtp.gmail.com:587",
                        ssl = true,
                        testTime = DateTime.Now,
                        targetEmail = testEmail
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gmail SMTP testi başarısız");

                // Gmail spesifik hata mesajları
                var troubleshootingSteps = new List<string>
                {
                    "1. Gmail hesabında 2-Step Verification açık olmalı",
                    "2. App Password oluşturulmalı (normal şifre değil)",
                    "3. Less secure app access kapatılmalı",
                    "4. Gmail hesabı engellenmiş olabilir",
                    "5. SMTP ayarları: smtp.gmail.com:587, SSL=true"
                };

                // Hata türüne göre özel öneriler ekle
                if (ex.Message.Contains("Authentication"))
                {
                    troubleshootingSteps.Add("6. App Password doğru mu? 16 karakterli olmalı");
                    troubleshootingSteps.Add("7. Email adresi doğru mu?");
                }
                else if (ex.Message.Contains("timeout") || ex.Message.Contains("connection"))
                {
                    troubleshootingSteps.Add("6. İnternet bağlantısı kontrol edin");
                    troubleshootingSteps.Add("7. Firewall SMTP trafiğini engelliyor olabilir");
                }

                return BadRequest(new
                {
                    success = false,
                    message = "Gmail SMTP testi başarısız: " + ex.Message,
                    troubleshooting = troubleshootingSteps,
                    timestamp = DateTime.Now,
                    errorType = ex.GetType().Name
                });
            }
        }

        // Toplu email gönderme testi
        [HttpPost("test-bulk")]
        public async Task<IActionResult> TestBulkEmail([FromBody] BulkEmailTestRequest request)
        {
            try
            {
                var results = new List<object>();

                foreach (var email in request.Emails)
                {
                    try
                    {
                        await _emailService.SendTestEmailAsync(email);
                        results.Add(new { email = email, success = true, message = "Başarılı" });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new { email = email, success = false, message = ex.Message });
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = $"{request.Emails.Count} email test edildi",
                    results = results,
                    summary = new
                    {
                        total = request.Emails.Count,
                        successful = results.Count(r => ((dynamic)r).success),
                        failed = results.Count(r => !((dynamic)r).success)
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Email formatı test et
        [HttpPost("test-format")]
        public async Task<IActionResult> TestEmailFormat([FromBody] EmailFormatTestRequest request)
        {
            try
            {
                var subject = $"🎨 Email Format Testi - {DateTime.Now:HH:mm}";
                var body = CreateFormattedTestEmail(request.TestType, request.Email);

                await _emailService.SendAdminNotificationAsync(subject, body);

                return Ok(new
                {
                    success = true,
                    message = "Email format testi gönderildi",
                    features = new[]
                    {
                        "HTML5 desteği",
                        "CSS3 stilleri",
                        "Emoji karakterleri",
                        "Responsive tasarım",
                        "Gradient arka planlar"
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // YENI: Sipariş tamamlandığında çağrılacak endpoint
        [HttpPost("send-order-emails")]
        [Authorize]
        public async Task<IActionResult> SendOrderEmails([FromBody] OrderEmailRequest request)
        {
            try
            {
                var results = new List<object>();

                // Müşteri onay e-postası gönder
                try
                {
                    await _emailService.SendOrderConfirmationAsync(
                        request.CustomerEmail,
                        request.OrderNumber,
                        request.TotalAmount
                    );
                    results.Add(new { type = "customer", success = true, message = "Müşteri onay e-postası gönderildi" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Müşteri onay e-postası gönderilemedi");
                    results.Add(new { type = "customer", success = false, message = ex.Message });
                }

                // Admin bildirim e-postası gönder
                try
                {
                    var adminNotification = CreateOrderNotificationContent(request);
                    await _emailService.SendNewOrderNotificationAsync(adminNotification);
                    results.Add(new { type = "admin", success = true, message = "Admin bildirim e-postası gönderildi" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Admin bildirim e-postası gönderilemedi");
                    results.Add(new { type = "admin", success = false, message = ex.Message });
                }

                var successCount = results.Count(r => ((dynamic)r).success);
                var totalCount = results.Count;

                return Ok(new
                {
                    success = successCount > 0,
                    message = $"{successCount}/{totalCount} e-posta başarıyla gönderildi",
                    results = results,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş e-postaları gönderilirken genel hata");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        #region Private Helper Methods

        private string CreateFormattedTestEmail(string testType, string email)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px;'>
                    <div style='background: white; padding: 30px; border-radius: 15px; box-shadow: 0 10px 30px rgba(0,0,0,0.2);'>
                        <h1 style='color: #333; text-align: center; margin-bottom: 30px;'>
                            🎨 Email Format Testi
                        </h1>
                        
                        <div style='background: #f8f9fa; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                            <h3 style='color: #495057; margin: 0 0 15px 0;'>📧 Test Detayları</h3>
                            <p><strong>Test Türü:</strong> {testType}</p>
                            <p><strong>Gönderim Zamanı:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>
                            <p><strong>Alıcı:</strong> {email}</p>
                        </div>

                        <div style='background: linear-gradient(45deg, #28a745, #20c997); color: white; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                            <h3 style='margin: 0 0 15px 0;'>✨ Format Özellikleri</h3>
                            <ul style='margin: 0; padding-left: 20px;'>
                                <li>📱 Responsive tasarım</li>
                                <li>🎨 Gradient arka planlar</li>
                                <li>🔤 Emoji desteği</li>
                                <li>📐 Modern CSS3 stilleri</li>
                                <li>🎯 Kolay okunabilirlik</li>
                            </ul>
                        </div>

                        <div style='text-align: center; padding: 20px; background: #e9ecef; border-radius: 10px;'>
                            <p style='margin: 0; font-size: 14px; color: #6c757d;'>
                                🚀 Bu email otomatik format testi için gönderilmiştir.<br>
                                💻 E-ticaret Backend Sistemi<br>
                                📅 {DateTime.Now:dd.MM.yyyy HH:mm}
                            </p>
                        </div>
                    </div>
                </div>";
        }

        private string CreateOrderNotificationContent(OrderEmailRequest request)
        {
            return $@"
                <h3>📋 Sipariş Bilgileri</h3>
                <p><strong>Sipariş Numarası:</strong> {request.OrderNumber}</p>
                <p><strong>Müşteri:</strong> {request.CustomerName}</p>
                <p><strong>E-posta:</strong> {request.CustomerEmail}</p>
                <p><strong>Telefon:</strong> {request.CustomerPhone}</p>
                <p><strong>Toplam Tutar:</strong> {request.TotalAmount:C}</p>
                <p><strong>Adres:</strong> {request.ShippingAddress}</p>
                <p><strong>Sipariş Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}</p>
                
                <h3>🛍️ Ürün Detayları</h3>
                <p>Ürün sayısı: {request.ItemCount}</p>
                <p>Detaylar sipariş yönetim panelinde görüntülenebilir.</p>";
        }

        #endregion

        // DTO Sınıfları
        public class TestEmailRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        public class TestOrderEmailRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        public class TestWelcomeEmailRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
        }

        public class AdminNotificationRequest
        {
            public string Subject { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }

        public class NewOrderNotificationRequest
        {
            public string OrderDetails { get; set; } = string.Empty;
        }

        public class BulkEmailTestRequest
        {
            public List<string> Emails { get; set; } = new List<string>();
        }

        public class EmailFormatTestRequest
        {
            public string Email { get; set; } = string.Empty;
            public string TestType { get; set; } = "Format Test";
        }

        // YENI: Sipariş e-posta gönderimi için DTO
        public class OrderEmailRequest
        {
            public string OrderNumber { get; set; } = string.Empty;
            public string CustomerName { get; set; } = string.Empty;
            public string CustomerEmail { get; set; } = string.Empty;
            public string CustomerPhone { get; set; } = string.Empty;
            public string ShippingAddress { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public int ItemCount { get; set; }
            public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
        }

        public class OrderItemDto
        {
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
        }
    }
}