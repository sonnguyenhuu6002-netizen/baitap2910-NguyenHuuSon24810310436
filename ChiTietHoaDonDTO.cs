namespace CafePOS.DTO
{
    public class ChiTietHoaDonDTO
    {
        public int MaHoaDon { get; set; }
        public int MaMon { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }
}
