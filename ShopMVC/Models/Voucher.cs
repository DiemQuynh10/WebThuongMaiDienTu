using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShopMVC.Models
{
    [Index(nameof(Code), IsUnique = true)]
    public class Voucher : IValidatableObject
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Code { get; set; } = null!;

        [Required, StringLength(200)]
        public string Ten { get; set; } = null!;

        [Range(0, 100)]
        public double? PhanTramGiam { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? GiamToiDa { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? GiamTrucTiep { get; set; }

        public DateTime NgayBatDau { get; set; }
        public DateTime NgayHetHan { get; set; }

        public int SoLanSuDungToiDa { get; set; } = 1;
        public int SoLanDaSuDung { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // --- QUAN TRỌNG: Cờ đánh dấu Flash Sale ---
        public bool IsFlashSale { get; set; } = false;

        public ICollection<VoucherThuongHieu> VoucherThuongHieus { get; set; } = new List<VoucherThuongHieu>();
        public ICollection<VoucherDanhMuc> VoucherDanhMucs { get; set; } = new List<VoucherDanhMuc>();

        // Quan hệ với bảng sản phẩm Flash Sale
        public ICollection<VoucherSanPham> VoucherSanPhams { get; set; } = new List<VoucherSanPham>();

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if ((PhanTramGiam == null || PhanTramGiam <= 0) && (GiamTrucTiep == null || GiamTrucTiep <= 0))
                yield return new ValidationResult("Chọn 1 hình thức giảm: Phần trăm hoặc Số tiền.", new[] { nameof(PhanTramGiam), nameof(GiamTrucTiep) });
        }
    }
}