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
        public string Code { get; set; } = null!;        // Ví dụ: SALE50

        [Required, StringLength(200)]
        public string Ten { get; set; } = null!;         // Tên chương trình

        [Range(0, 100)]
        public double? PhanTramGiam { get; set; }        // Giảm %

        [Range(0, double.MaxValue)]
        public decimal? GiamToiDa { get; set; }          // Trần giảm khi dùng %

        [Range(0, double.MaxValue)]
        public decimal? GiamTrucTiep { get; set; }       // Giảm số tiền

        [DataType(DataType.Date)]
        public DateTime NgayBatDau { get; set; }

        [DataType(DataType.Date)]
        public DateTime NgayHetHan { get; set; }

        [Range(0, int.MaxValue)]
        public int SoLanSuDungToiDa { get; set; } = 1;   // 0 = không giới hạn

        [Range(0, int.MaxValue)]
        public int SoLanDaSuDung { get; set; } = 0;

        public bool IsActive { get; set; } = true;


        // Rule: bắt buộc chọn 1 trong 2 hình thức giảm (%, hoặc số tiền)
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if ((PhanTramGiam == null || PhanTramGiam <= 0) &&
                (GiamTrucTiep == null || GiamTrucTiep <= 0))
            {
                yield return new ValidationResult(
                    "Chọn 1 hình thức giảm: Phần trăm hoặc Số tiền.",
                    new[] { nameof(PhanTramGiam), nameof(GiamTrucTiep) });
            }

            if (PhanTramGiam is > 0 && GiamTrucTiep is > 0)
            {
                yield return new ValidationResult(
                    "Chỉ được chọn 1 hình thức giảm (không vừa % vừa số tiền).",
                    new[] { nameof(PhanTramGiam), nameof(GiamTrucTiep) });
            }

            if (NgayHetHan < NgayBatDau)
            {
                yield return new ValidationResult(
                    "Ngày hết hạn phải lớn hơn hoặc bằng ngày bắt đầu.",
                    new[] { nameof(NgayHetHan) });
            }
        }
        public ICollection<VoucherThuongHieu> VoucherThuongHieus { get; set; } = new List<VoucherThuongHieu>();
        public ICollection<VoucherDanhMuc> VoucherDanhMucs { get; set; } = new List<VoucherDanhMuc>();
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
