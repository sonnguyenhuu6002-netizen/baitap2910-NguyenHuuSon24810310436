namespace CafePOS.DTO
{
    public class BanDTO
    {
        public int MaBan { get; set; }
        public string TenBan { get; set; }
        public string TrangThai { get; set; }      // "Trống" | "Có khách"
        public bool Trong => TrangThai == "Trống";
    }
}
