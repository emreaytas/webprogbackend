using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using webprogbackend.Models.Enums;

namespace webprogbackend.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [JsonIgnore] // Don't serialize password in API responses
        public string Password { get; set; }

        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRole Role { get; set; } = UserRole.User;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual Cart Cart { get; set; }

        // Helper methods for role checking
        public bool IsAdmin() => Role == UserRole.Admin;
        public bool IsModerator() => Role == UserRole.Moderator;
        public bool IsUser() => Role == UserRole.User;
        public bool HasAdminOrModeratorRole() => Role == UserRole.Admin || Role == UserRole.Moderator;
    }
}