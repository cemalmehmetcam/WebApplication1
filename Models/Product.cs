using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // <-- BU SATIRI UNUTMA

namespace WebApplication1.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ürün adı zorunludur.")]
        [Display(Name = "Ürün Adı")]
        public string Name { get; set; }

        [Display(Name = "Açıklama")]
        public string Description { get; set; }

        // --- BURAYI DEĞİŞTİRİYORUZ ---
        [Required(ErrorMessage = "Fiyat zorunludur.")]
        [Display(Name = "Fiyat")]
        [Column(TypeName = "decimal(18, 2)")] // <-- BU SATIRI EKLE (18 basamak, 2'si kuruş)
        public decimal Price { get; set; }
        // -----------------------------

        [Display(Name = "Resim URL")]
        public string ImageUrl { get; set; }

        [Display(Name = "Kategori")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }
    }
}