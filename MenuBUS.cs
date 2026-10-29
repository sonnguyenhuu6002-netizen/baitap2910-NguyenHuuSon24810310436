using System;
using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using CafePOS.DAL;
using CafePOS.DTO;

namespace CafePOS.BUS
{
    public static class MenuBUS
    {
        public static DataSet LoadMenuDataSet() => MenuDAL.LoadMenuDataSet();
        public static DataTable GetMonAnView() => MenuDAL.GetMonAnView();

        // ---------- Validation ----------
        private static string CheckTen(string ten, string label)
        {
            if (string.IsNullOrWhiteSpace(ten))
                throw new ArgumentException($"{label} không được để trống.");
            return ten.Trim();
        }

        private static decimal ParseGia(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Đơn giá không được để trống.");
            if (!decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out decimal gia))
                throw new ArgumentException("Đơn giá phải là một số hợp lệ.");
            if (gia < 0) throw new ArgumentException("Đơn giá không được âm.");
            return gia;
        }

        // Bat loi khoa ngoai (547) -> thong bao than thien
        private static void Run(Action a, string msgFk)
        {
            try { a(); }
            catch (SqlException ex) when (ex.Number == 547) { throw new InvalidOperationException(msgFk); }
        }

        // ---------- Danh muc ----------
        public static void ThemDanhMuc(string ten) =>
            MenuDAL.InsertDanhMuc(new DanhMucDTO { TenDanhMuc = CheckTen(ten, "Tên danh mục") });

        public static void SuaDanhMuc(int ma, string ten) =>
            MenuDAL.UpdateDanhMuc(new DanhMucDTO { MaDanhMuc = ma, TenDanhMuc = CheckTen(ten, "Tên danh mục") });

        public static void XoaDanhMuc(int ma) =>
            Run(() => MenuDAL.DeleteDanhMuc(ma), "Không thể xóa: danh mục vẫn còn món ăn.");

        // ---------- Mon an ----------
        public static void ThemMon(string ten, int maDM, string gia) =>
            MenuDAL.InsertMon(new MonAnDTO
            { TenMon = CheckTen(ten, "Tên món"), MaDanhMuc = maDM, DonGia = ParseGia(gia) });

        public static void SuaMon(int ma, string ten, int maDM, string gia) =>
            MenuDAL.UpdateMon(new MonAnDTO
            { MaMon = ma, TenMon = CheckTen(ten, "Tên món"), MaDanhMuc = maDM, DonGia = ParseGia(gia) });

        public static void XoaMon(int ma) =>
            Run(() => MenuDAL.DeleteMon(ma), "Không thể xóa: món này đã có trong hóa đơn.");
    }
}
