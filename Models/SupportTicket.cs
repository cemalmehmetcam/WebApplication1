using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class SupportTicket
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        [Required]
        public string Subject { get; set; } // Konu: İade, Adres Değişikliği vb.

        [Required]
        public string Message { get; set; }

        public string Status { get; set; } = "Beklemede"; // Beklemede, Cevaplandı, Kapandı

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}