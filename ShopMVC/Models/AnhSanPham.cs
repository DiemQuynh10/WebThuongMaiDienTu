using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopMVC.Models
{
    public class AnhSanPham
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int IdSanPham { get; set; }

        [ForeignKey(nameof(IdSanPham))]
        public SanPham? SanPham { get; set; }

        [Required, StringLength(500)]
        public string Url { get; set; } = string.Empty;

        public bool LaAnhChinh { get; set; } = false;
        public int ThuTu { get; set; } = 0;
    }
}
