using System;
using System.Data;
using CafePOS.DAL;
using CafePOS.DTO;

namespace CafePOS.BUS
{
    public static class HoaDonBUS
    {
        public static HoaDonDTO GetHoaDonChuaThanhToan(int maBan) => HoaDonDAL.GetHoaDonChuaThanhToan(maBan);
        public static DataTable GetChiTiet(int maHD) => HoaDonDAL.GetChiTiet(maHD);

        public static void ThemMon(int maBan, int maMon, int soLuong)
        {
            if (soLuong <= 0) throw new ArgumentException("Số lượng phải lớn hơn 0.");
            HoaDonDAL.ThemMon(maBan, maMon, soLuong);
        }

        public static decimal ThanhToan(int? maHD, int maBan)
        {
            if (maHD == null) throw new InvalidOperationException("Bàn này chưa có hóa đơn để thanh toán.");
            return HoaDonDAL.ThanhToan(maHD.Value, maBan);
        }

        public static void ChuyenBan(int? maHD, int banCu, int banMoi)
        {
            if (maHD == null) throw new InvalidOperationException("Bàn hiện tại chưa có hóa đơn.");
            if (banCu == banMoi) throw new ArgumentException("Bàn đích phải khác bàn hiện tại.");
            HoaDonDAL.ChuyenBan(maHD.Value, banCu, banMoi);
        }

        // ---------- Thong ke ----------
        private static void CheckKhoang(DateTime tu, DateTime den)
        {
            if (tu.Date > den.Date) throw new ArgumentException("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
        }

        public static DataTable ThongKeDoanhThuTheoNgay(DateTime tu, DateTime den)
        { CheckKhoang(tu, den); return HoaDonDAL.ThongKeDoanhThuTheoNgay(tu, den); }

        public static DataTable ThongKeTopMon(DateTime tu, DateTime den)
        { CheckKhoang(tu, den); return HoaDonDAL.ThongKeTopMon(tu, den); }
    }
}
