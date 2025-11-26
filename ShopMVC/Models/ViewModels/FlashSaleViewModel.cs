using System;
using System.Collections.Generic;

namespace ShopMVC.Models.ViewModels
{
    public class FlashSaleViewModel
    {
        public int VoucherId { get; set; }
        public string TenChuongTrinh { get; set; }
        public DateTime EndTime { get; set; }
        public List<FlashSaleItemViewModel> Items { get; set; } = new List<FlashSaleItemViewModel>();
    }

    public class FlashSaleItemViewModel
    {
        public SanPham SanPham { get; set; }
        public decimal GiaGoc { get; set; }
        public decimal GiaSale { get; set; }
        public int SoLuongPhanBo { get; set; }
        public int SoLuongDaBan { get; set; }

        public int PhanTramDaBan
        {
            get
            {
                if (SoLuongPhanBo <= 0) return 100;
                var pct = (int)((double)SoLuongDaBan / SoLuongPhanBo * 100);
                return pct > 100 ? 100 : pct;
            }
        }
    }
}