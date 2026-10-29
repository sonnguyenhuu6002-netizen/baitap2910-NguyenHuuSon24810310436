using System.Data;
using Microsoft.Data.SqlClient;
using CafePOS.DTO;

namespace CafePOS.DAL
{
    public static class MenuDAL
    {
        /// <summary>Nap DanhMuc + MonAn vao 1 DataSet, co DataRelation.</summary>
        public static DataSet LoadMenuDataSet()
        {
            var ds = new DataSet("Menu");
            using (var conn = DataProvider.CreateConnection())
            {
                new SqlDataAdapter("SELECT MaDanhMuc, TenDanhMuc FROM DanhMuc ORDER BY TenDanhMuc", conn)
                    .Fill(ds, "DanhMuc");
                new SqlDataAdapter("SELECT MaMon, TenMon, MaDanhMuc, DonGia FROM MonAn ORDER BY TenMon", conn)
                    .Fill(ds, "MonAn");
            }
            ds.Relations.Add("DM_Mon",
                ds.Tables["DanhMuc"].Columns["MaDanhMuc"],
                ds.Tables["MonAn"].Columns["MaDanhMuc"], false);
            return ds;
        }

        // ---------- Danh muc ----------
        public static int InsertDanhMuc(DanhMucDTO dm) =>
            DataProvider.ExecuteNonQuery("INSERT DanhMuc(TenDanhMuc) VALUES (@t)",
                new SqlParameter("@t", dm.TenDanhMuc));

        public static int UpdateDanhMuc(DanhMucDTO dm) =>
            DataProvider.ExecuteNonQuery("UPDATE DanhMuc SET TenDanhMuc=@t WHERE MaDanhMuc=@id",
                new SqlParameter("@t", dm.TenDanhMuc), new SqlParameter("@id", dm.MaDanhMuc));

        public static int DeleteDanhMuc(int ma) =>
            DataProvider.ExecuteNonQuery("DELETE DanhMuc WHERE MaDanhMuc=@id",
                new SqlParameter("@id", ma));

        // ---------- Mon an ----------
        public static DataTable GetMonAnView() =>
            DataProvider.ExecuteQuery(
                @"SELECT m.MaMon, m.TenMon, m.MaDanhMuc, d.TenDanhMuc, m.DonGia
                  FROM MonAn m JOIN DanhMuc d ON d.MaDanhMuc = m.MaDanhMuc
                  ORDER BY d.TenDanhMuc, m.TenMon");

        private static SqlParameter Gia(decimal g) =>
            new SqlParameter("@g", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = g };

        public static int InsertMon(MonAnDTO m) =>
            DataProvider.ExecuteNonQuery(
                "INSERT MonAn(TenMon, MaDanhMuc, DonGia) VALUES (@t,@dm,@g)",
                new SqlParameter("@t", m.TenMon), new SqlParameter("@dm", m.MaDanhMuc), Gia(m.DonGia));

        public static int UpdateMon(MonAnDTO m) =>
            DataProvider.ExecuteNonQuery(
                "UPDATE MonAn SET TenMon=@t, MaDanhMuc=@dm, DonGia=@g WHERE MaMon=@id",
                new SqlParameter("@t", m.TenMon), new SqlParameter("@dm", m.MaDanhMuc), Gia(m.DonGia),
                new SqlParameter("@id", m.MaMon));

        public static int DeleteMon(int ma) =>
            DataProvider.ExecuteNonQuery("DELETE MonAn WHERE MaMon=@id", new SqlParameter("@id", ma));
    }
}
