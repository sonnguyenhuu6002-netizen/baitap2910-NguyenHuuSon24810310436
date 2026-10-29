namespace HotelManagement.DTO
{
    public class PhongDTO
    {
        public string SoPhong { get; set; }
        public string LoaiPhong { get; set; }
        public decimal GiaTheoGio { get; set; }
        public string TrangThai { get; set; }
    }

    public class KhachHangDTO
    {
        public int MaKH { get; set; }
        public string HoTen { get; set; }
        public string CCCD { get; set; }
        public string SoDienThoai { get; set; }
    }

    public class DichVuDTO
    {
        public int MaDV { get; set; }
        public string TenDV { get; set; }
        public decimal DonGia { get; set; }
    }

    public class PhieuDatPhongDTO
    {
        public int MaPhieu { get; set; }
        public string SoPhong { get; set; }
        public int MaKH { get; set; }
        public DateTime NgayNhanPhong { get; set; }
        public DateTime? NgayTraPhong { get; set; }
        public decimal? TongTienThanhToan { get; set; }
    }

    public class SuDungDichVuDTO
    {
        public int MaPhieu { get; set; }
        public int MaDV { get; set; }
        public int SoLuong { get; set; }
        public DateTime ThoiGian { get; set; }
    }

    /// <summary>DTO mang kết quả tính tiền khi check-out.</summary>
    public class HoaDonDTO
    {
        public int MaPhieu { get; set; }
        public string SoPhong { get; set; }
        public string HoTen { get; set; }
        public DateTime NgayNhan { get; set; }
        public DateTime NgayTra { get; set; }
        public int SoGio { get; set; }
        public decimal GiaTheoGio { get; set; }
        public decimal TienPhong { get; set; }
        public decimal TienDichVu { get; set; }
        public decimal TongTien { get; set; }
    }
}
