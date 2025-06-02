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

        // Sipariş onay maili
        public async Task SendOrderConfirmationAsync(string customerEmail, string orderNumber, decimal totalAmount)
        {
            var subject = $"✅ Sipariş Onayı - {orderNumber}";

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #28a745;'>🎉 Siparişiniz Alındı!</h2>
                    
                    <p>Merhaba,</p>
                    
                    <p>Siparişiniz başarıyla alınmıştır ve işleme alınmıştır.</p>
                    
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3>📋 Sipariş Detayları</h3>
                        <p><strong>Sipariş Numarası:</strong> {orderNumber}</p>
                        <p><strong>Toplam Tutar:</strong> {totalAmount:C}</p>
                        <p><strong>Sipariş Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}</p>
                    </div>
                    
                    <p>Siparişinizin kargo durumunu takip edebilirsiniz.</p>
                    
                    <p style='color: #666; font-size: 14px;'>
                        Teşekkür ederiz! 🛒<br>
                        E-Commerce Ekibi
                    </p>
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
            var adminEmail = _configuration["AdminSettings:DefaultAdminEmail"];

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #fd7e14;'>🔔 Admin Bildirimi</h2>
                    
                    <div style='background-color: #fff3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                        {message}
                    </div>
                    
                    <p style='color: #666; font-size: 14px;'>
                        Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}<br>
                        Sistem: E-Commerce Backend
                    </p>
                </div>";

            await SendEmailAsync(adminEmail, $"[ADMIN] {subject}", body);
        }

        // Ana email gönderme metodu
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Email ayarlarını configuration'dan al
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:Port"]);
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromPassword = _configuration["EmailSettings:Password"];
                var fromName = _configuration["EmailSettings:FromName"];

                // SMTP client oluştur
                using var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true,
                    Timeout = 10000 // 10 saniye timeout
                };

                // Email mesajı oluştur
                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                // Email'i gönder
                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email başarıyla gönderildi: {Email} - {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email gönderilirken hata oluştu: {Email} - {Subject}", toEmail, subject);

                // Hata durumunda exception fırlatma, sadece logla
                // Bu sayede sistem çökmez, sadece email gönderilmez
            }
        }

        // Test email metodu
        public async Task SendTestEmailAsync(string testEmail)
        {
            var subject = "🧪 Test Email - Sistem Çalışıyor!";

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #28a745;'>✅ Test Başarılı!</h2>
                    
                    <p>Bu bir test emailidir.</p>
                    
                    <div style='background-color: #d4edda; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <p><strong>✅ Email sistemi çalışıyor!</strong></p>
                        <p>Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}</p>
                    </div>
                    
                    <p style='color: #666; font-size: 14px;'>
                        Bu test emailini aldıysanız, sistem doğru çalışıyor demektir.
                    </p>
                </div>";

            await SendEmailAsync(testEmail, subject, body);
        }
    }
}