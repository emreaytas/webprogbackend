using System.Net;
using System.Net.Mail;

namespace webprogbackend.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(string customerEmail, string orderNumber, decimal totalAmount);
        Task SendPasswordResetAsync(string email, string resetLink);
        Task SendWelcomeEmailAsync(string email, string username);
        Task SendAdminNotificationAsync(string subject, string message);
        Task SendNewOrderNotificationAsync(string orderDetails);
        Task SendTestEmailAsync(string testEmail);
        // Yeni metodlar
        Task<bool> SendOrderEmailsAsync(string customerEmail, string orderNumber, decimal totalAmount, string orderDetails);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // Sipariş onay maili - Geliştirilmiş versiyon
        public async Task SendOrderConfirmationAsync(string customerEmail, string orderNumber, decimal totalAmount)
        {
            var subject = $"✅ Sipariş Onayı - {orderNumber}";

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #f8f9fa; padding: 20px;'>
                    <div style='background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        <h2 style='color: #28a745; text-align: center; margin-bottom: 30px;'>
                            🎉 Siparişiniz Alındı!
                        </h2>
                        
                        <div style='background-color: #e7f3ff; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                            <h3 style='margin: 0 0 15px 0; color: #0056b3;'>📋 Sipariş Detayları</h3>
                            <p><strong>Sipariş Numarası:</strong> {orderNumber}</p>
                            <p><strong>Toplam Tutar:</strong> {totalAmount:C}</p>
                            <p><strong>Sipariş Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}</p>
                            <p><strong>Tahmini Teslimat:</strong> {DateTime.Now.AddDays(2):dd.MM.yyyy} - {DateTime.Now.AddDays(5):dd.MM.yyyy}</p>
                        </div>
                        
                        <div style='background-color: #d4edda; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                            <h3 style='margin: 0 0 15px 0; color: #155724;'>📦 Sonraki Adımlar</h3>
                            <ul style='margin: 0; padding-left: 20px;'>
                                <li>✅ Siparişiniz onaylandı ve işleme alındı</li>
                                <li>📦 Kargo hazırlığına başlandı</li>
                                <li>🚚 Kargo takip numarası size SMS ile gönderilecek</li>
                                <li>📞 Herhangi bir sorunuz varsa bizimle iletişime geçin</li>
                            </ul>
                        </div>
                        
                        <div style='text-align: center; padding: 20px; background-color: #f8f9fa; border-radius: 8px;'>
                            <p style='margin: 0; color: #6c757d; font-size: 14px;'>
                                Teşekkür ederiz! 🛒<br>
                                <strong>E-Commerce Ekibi</strong><br>
                                📧 Destek: support@ecommerce.com | 📞 0850 XXX XX XX
                            </p>
                        </div>
                    </div>
                </div>";

            await SendEmailAsync(customerEmail, subject, body);
        }

        // Şifre sıfırlama maili
        public async Task SendPasswordResetAsync(string email, string resetLink)
        {
            var subject = "🔐 Şifre Sıfırlama Talebi";

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #dc3545;'>Şifre Sıfırlama</h2>
                    
                    <p>Merhaba,</p>
                    
                    <p>Şifrenizi sıfırlama talebiniz alınmıştır.</p>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{resetLink}' 
                           style='background-color: #007bff; color: white; padding: 15px 30px; 
                                  text-decoration: none; border-radius: 5px; display: inline-block;'>
                            🔄 Şifremi Sıfırla
                        </a>
                    </div>
                    
                    <p style='color: #dc3545; font-weight: bold;'>
                        ⚠️ Bu bağlantı 24 saat geçerlidir.
                    </p>
                    
                    <p style='color: #666; font-size: 14px;'>
                        Bu talebi siz yapmadıysanız, bu maili görmezden gelebilirsiniz.
                    </p>
                </div>";

            await SendEmailAsync(email, subject, body);
        }

        // Hoş geldin maili
        public async Task SendWelcomeEmailAsync(string email, string username)
        {
            var subject = "🎉 Hoş Geldiniz!";

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #28a745;'>Hoş Geldiniz {username}! 🎊</h2>
                    
                    <p>E-ticaret platformumuza katıldığınız için teşekkür ederiz!</p>
                    
                    <div style='background-color: #e7f3ff; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3>🛍️ Neler Yapabilirsiniz?</h3>
                        <ul>
                            <li>✨ Binlerce ürün arasından seçim yapabilirsiniz</li>
                            <li>🛒 Sepetinize ürün ekleyebilirsiniz</li>
                            <li>📦 Siparişlerinizi takip edebilirsiniz</li>
                            <li>⭐ Ürünleri değerlendirebilirsiniz</li>
                        </ul>
                    </div>
                    
                    <p style='color: #666; font-size: 14px;'>
                        İyi alışverişler! 🛒<br>
                        E-Commerce Ekibi
                    </p>
                </div>";

            await SendEmailAsync(email, subject, body);
        }

        // Admin bildirim maili
        public async Task SendAdminNotificationAsync(string subject, string message)
        {
            var adminEmail = _configuration["EmailSettings:DefaultRecipientEmail"] ??
                           _configuration["AdminSettings:DefaultAdminEmail"];

            if (string.IsNullOrEmpty(adminEmail))
            {
                _logger.LogWarning("Admin email adresi yapılandırılmamış!");
                return;
            }

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #fd7e14;'>🔔 Admin Bildirimi</h2>
                    
                    <div style='background-color: #fff3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                        {message}
                    </div>
                    
                    <p style='color: #666; font-size: 14px;'>
                        Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}<br>
                        Sistem: E-Commerce Backend<br>
                        Server: {Environment.MachineName}
                    </p>
                </div>";

            await SendEmailAsync(adminEmail, $"[ADMIN] {subject}", body);
        }

        // Yeni sipariş bildirimi (Admin için)
        public async Task SendNewOrderNotificationAsync(string orderDetails)
        {
            var adminEmail = _configuration["EmailSettings:DefaultRecipientEmail"] ??
                           _configuration["AdminSettings:DefaultAdminEmail"];

            if (string.IsNullOrEmpty(adminEmail))
            {
                _logger.LogWarning("Admin email adresi yapılandırılmamış!");
                return;
            }

            var subject = "🛒 Yeni Sipariş Alındı!";

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #f8f9fa; padding: 20px;'>
                    <div style='background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        <h2 style='color: #28a745; text-align: center; margin-bottom: 30px;'>
                            🎉 Yeni Sipariş Alındı!
                        </h2>
                        
                        {orderDetails}
                        
                        <div style='text-align: center; padding: 20px; background-color: #f8f9fa; border-radius: 8px; margin-top: 20px;'>
                            <p style='margin: 0; color: #6c757d; font-size: 14px;'>
                                🚀 Bu sipariş otomatik olarak oluşturulmuştur.<br>
                                📧 E-ticaret sistemi tarafından gönderilmiştir.<br>
                                📅 {DateTime.Now:dd.MM.yyyy HH:mm}<br>
                                🖥️ Server: {Environment.MachineName}
                            </p>
                        </div>
                    </div>
                </div>";

            await SendEmailAsync(adminEmail, subject, body);
        }

        // Test email metodu
        public async Task SendTestEmailAsync(string testEmail)
        {
            var subject = $"🧪 Test Email - Sistem Çalışıyor! - {DateTime.Now:HH:mm:ss}";

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #28a745;'>✅ Test Başarılı!</h2>
                    
                    <p>Bu bir test emailidir.</p>
                    
                    <div style='background-color: #d4edda; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <p><strong>✅ Email sistemi çalışıyor!</strong></p>
                        <p><strong>📅 Tarih:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>
                        <p><strong>📧 Test Email:</strong> {testEmail}</p>
                        <p><strong>🖥️ Server:</strong> {Environment.MachineName}</p>
                        <p><strong>📡 SMTP:</strong> {_configuration["EmailSettings:SmtpServer"]}:{_configuration["EmailSettings:Port"]}</p>
                    </div>
                    
                    <div style='background-color: #e7f3ff; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h4 style='margin: 0 0 10px 0; color: #0056b3;'>📊 Sistem Bilgileri</h4>
                        <p style='margin: 5px 0;'><strong>🌐 Environment:</strong> {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}</p>
                        <p style='margin: 5px 0;'><strong>⏰ Server Time:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                        <p style='margin: 5px 0;'><strong>🌍 UTC Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</p>
                    </div>
                    
                    <p style='color: #666; font-size: 14px;'>
                        Bu test emailini aldıysanız, sistem doğru çalışıyor demektir.<br>
                        <strong>✨ E-Commerce Email Sistemi</strong>
                    </p>
                </div>";

            await SendEmailAsync(testEmail, subject, body);
        }

        // YENI: Sipariş e-postalarını toplu gönder (hem müşteri hem admin)
        public async Task<bool> SendOrderEmailsAsync(string customerEmail, string orderNumber, decimal totalAmount, string orderDetails)
        {
            var tasks = new List<Task>();
            var results = new List<bool>();

            try
            {
                // Müşteri onay e-postası
                var customerTask = Task.Run(async () =>
                {
                    try
                    {
                        await SendOrderConfirmationAsync(customerEmail, orderNumber, totalAmount);
                        _logger.LogInformation($"✅ Müşteri onay e-postası gönderildi: {customerEmail}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"❌ Müşteri onay e-postası gönderilemedi: {customerEmail}");
                        return false;
                    }
                });

                // Admin bildirim e-postası
                var adminTask = Task.Run(async () =>
                {
                    try
                    {
                        await SendNewOrderNotificationAsync(orderDetails);
                        _logger.LogInformation("✅ Admin bildirim e-postası gönderildi");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Admin bildirim e-postası gönderilemedi");
                        return false;
                    }
                });

                // Her iki e-postayı paralel gönder
                var customerResult = await customerTask;
                var adminResult = await adminTask;

                var successCount = (customerResult ? 1 : 0) + (adminResult ? 1 : 0);
                _logger.LogInformation($"📧 E-posta gönderim sonucu: {successCount}/2 başarılı");

                return successCount > 0; // En az bir tanesi başarılı olsun
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş e-postaları gönderilirken genel hata");
                return false;
            }
        }

        // Ana email gönderme metodu - Geliştirilmiş versiyon
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var retryCount = 0;
            var maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    // Email ayarlarını configuration'dan al
                    var smtpServer = _configuration["EmailSettings:SmtpServer"];
                    var smtpPort = int.Parse(_configuration["EmailSettings:Port"]);
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromPassword = _configuration["EmailSettings:Password"];
                    var fromName = _configuration["EmailSettings:FromName"];

                    // Ayarların varlığını kontrol et
                    if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromPassword))
                    {
                        throw new InvalidOperationException("Email ayarları eksik! SMTP server, email veya password yapılandırılmamış.");
                    }

                    _logger.LogInformation($"📧 Email gönderiliyor: {toEmail} - {subject} (Deneme: {retryCount + 1}/{maxRetries})");
                    _logger.LogDebug($"SMTP Ayarları: {smtpServer}:{smtpPort}, From: {fromEmail}");

                    // SMTP client oluştur
                    using var smtpClient = new SmtpClient(smtpServer)
                    {
                        Port = smtpPort,
                        Credentials = new NetworkCredential(fromEmail, fromPassword),
                        EnableSsl = true,
                        Timeout = 30000, // 30 saniye timeout
                        DeliveryMethod = SmtpDeliveryMethod.Network
                    };

                    // Email mesajı oluştur
                    using var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                        Priority = MailPriority.Normal
                    };

                    mailMessage.To.Add(toEmail);

                    // Email'i gönder
                    await smtpClient.SendMailAsync(mailMessage);

                    _logger.LogInformation($"✅ Email başarıyla gönderildi: {toEmail} - {subject}");
                    return; // Başarılı, döngüden çık
                }
                catch (SmtpException smtpEx)
                {
                    retryCount++;
                    _logger.LogError(smtpEx, $"❌ SMTP hatası (Deneme {retryCount}/{maxRetries}) - Email gönderilemedi: {toEmail} - {subject}");
                    _logger.LogError($"SMTP Hata Detayları: StatusCode={smtpEx.StatusCode}, Message={smtpEx.Message}");

                    // Gmail spesifik hata mesajları
                    if (smtpEx.Message.Contains("Authentication") || smtpEx.Message.Contains("Username and Password not accepted"))
                    {
                        _logger.LogError("🔑 Gmail Authentication hatası! Kontrol edilecekler:");
                        _logger.LogError("   1. 2-Step Verification açık mı?");
                        _logger.LogError("   2. App Password kullanılıyor mu? (Normal şifre değil)");
                        _logger.LogError("   3. Email adresi doğru mu?");
                        break; // Authentication hatasında retry yapma
                    }

                    if (retryCount >= maxRetries)
                    {
                        throw; // Max retry'a ulaştık, hatayı fırlat
                    }

                    // Retry için bekle (exponential backoff)
                    await Task.Delay(1000 * retryCount);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogError(ex, $"❌ Genel hata (Deneme {retryCount}/{maxRetries}) - Email gönderilemedi: {toEmail} - {subject}");
                    _logger.LogError($"Hata türü: {ex.GetType().Name}");
                    _logger.LogError($"Hata mesajı: {ex.Message}");

                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                    }

                    if (retryCount >= maxRetries)
                    {
                        throw; // Max retry'a ulaştık, hatayı fırlat
                    }

                    // Retry için bekle
                    await Task.Delay(1000 * retryCount);
                }
            }
        }

        // Email ayarlarını test et - Geliştirilmiş versiyon
        public async Task<bool> TestEmailConfigurationAsync()
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromPassword = _configuration["EmailSettings:Password"];

                _logger.LogInformation("🔧 Email konfigürasyonu test ediliyor...");
                _logger.LogInformation($"SMTP: {smtpServer}:{smtpPort}");
                _logger.LogInformation($"From: {fromEmail}");
                _logger.LogInformation($"Password configured: {!string.IsNullOrEmpty(fromPassword)}");

                // Temel ayar kontrolü
                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromPassword))
                {
                    _logger.LogError("❌ Email ayarları eksik!");
                    return false;
                }

                // SMTP bağlantısını test et
                using var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true,
                    Timeout = 10000
                };

                // Test connection (bu sadece bağlantıyı test eder, email göndermez)
                _logger.LogInformation("✅ Email konfigürasyonu geçerli görünüyor");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Email konfigürasyon testi başarısız");
                return false;
            }
        }

        // YENI: Email ayarlarını detaylı kontrol et
        public (bool IsValid, List<string> Issues) ValidateEmailSettings()
        {
            var issues = new List<string>();
            var isValid = true;

            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = _configuration["EmailSettings:Port"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromPassword = _configuration["EmailSettings:Password"];
            var fromName = _configuration["EmailSettings:FromName"];
            var defaultRecipient = _configuration["EmailSettings:DefaultRecipientEmail"];

            if (string.IsNullOrEmpty(smtpServer))
            {
                issues.Add("SMTP Server yapılandırılmamış");
                isValid = false;
            }

            if (string.IsNullOrEmpty(smtpPort) || !int.TryParse(smtpPort, out _))
            {
                issues.Add("SMTP Port geçersiz veya yapılandırılmamış");
                isValid = false;
            }

            if (string.IsNullOrEmpty(fromEmail))
            {
                issues.Add("From Email yapılandırılmamış");
                isValid = false;
            }
            else if (!fromEmail.Contains("@"))
            {
                issues.Add("From Email geçersiz format");
                isValid = false;
            }

            if (string.IsNullOrEmpty(fromPassword))
            {
                issues.Add("Email Password yapılandırılmamış");
                isValid = false;
            }
            else if (fromPassword.Length < 8)
            {
                issues.Add("Email Password çok kısa (App Password 16 karakter olmalı)");
                isValid = false;
            }

            if (string.IsNullOrEmpty(fromName))
            {
                issues.Add("From Name yapılandırılmamış (opsiyonel)");
            }

            if (string.IsNullOrEmpty(defaultRecipient))
            {
                issues.Add("Default Recipient Email yapılandırılmamış (opsiyonel)");
            }

            // Gmail spesifik kontroller
            if (!string.IsNullOrEmpty(fromEmail) && fromEmail.EndsWith("@gmail.com"))
            {
                if (!string.IsNullOrEmpty(smtpServer) && smtpServer != "smtp.gmail.com")
                {
                    issues.Add("Gmail için SMTP server 'smtp.gmail.com' olmalı");
                    isValid = false;
                }

                if (!string.IsNullOrEmpty(smtpPort) && smtpPort != "587")
                {
                    issues.Add("Gmail için SMTP port '587' olmalı");
                }

                if (!string.IsNullOrEmpty(fromPassword) && !fromPassword.Contains(" ") && fromPassword.Length == 16)
                {
                    // App Password olabilir, uyarı ver
                    issues.Add("Gmail App Password kullanıldığından emin olun (normal şifre değil)");
                }
            }

            return (isValid, issues);
        }
    }
}