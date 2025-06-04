using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using webprogbackend.Models.Enums;

namespace webprogbackend.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullan�c� ad� zorunludur")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullan�c� ad� 3-50 karakter aras�nda olmal�d�r")]
        public string Username { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur")]
        [EmailAddress(ErrorMessage = "Ge�erli bir e-posta adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "�ifre zorunludur")]
        [JsonIgnore] // API yan�tlar�nda �ifreyi gizle
        public string Password { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.User;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        // Helper methods
        public bool IsAdmin() => Role == UserRole.Admin;
        public bool IsUser() => Role == UserRole.User;
    }
}
