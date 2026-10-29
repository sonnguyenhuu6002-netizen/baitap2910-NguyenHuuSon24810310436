using System;

namespace CafePOS.DTO
{
    public class HoaDonDTO
    {
        public int MaHoaDon { get; set; }
        public int MaBan { get; set; }
        public DateTime GioVao { get; set; }
        public DateTime? GioRa { get; set; }
        public decimal TongTien { get; set; }
        public bool TrangThai { get; set; }        // false: chua thanh toan, true: da thanh toan
    }
}
