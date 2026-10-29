using System;
using System.Data;
using Microsoft.Data.SqlClient;
using CafePOS.DTO;

namespace CafePOS.DAL
{
    public static class HoaDonDAL
    {
        private const string SQL_HD_CHUA_TT =
            "SELECT TOP 1 MaHoaDon FROM HoaDon WHERE MaBan=@b AND TrangThai=0 ORDER BY MaHoaDon DESC";

        /// <summary>Hoa don chua thanh toan cua ban (null neu chua co).</summary>
        public static HoaDonDTO GetHoaDonChuaThanhToan(int maBan)
        {
            var dt = DataProvider.ExecuteQuery(
                @"SELECT TOP 1 MaHoaDon, MaBan, GioVao, GioRa, TongTien, TrangThai
                  FROM HoaDon WHERE MaBan=@b AND TrangThai=0 ORDER BY MaHoaDon DESC",
                new SqlParameter("@b", maBan));
            if (dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];
            return new HoaDonDTO
            {
                MaHoaDon = (int)r["MaHoaDon"],
                MaBan = (int)r["MaBan"],
                GioVao = (DateTime)r["GioVao"],
                GioRa = r["GioRa"] == DBNull.Value ? (DateTime?)null : (DateTime)r["GioRa"],
                TongTien = (decimal)r["TongTien"],
                TrangThai = (bool)r["TrangThai"]
            };
        }

        // ---------- Bao cao - Thong ke ----------
        public static DataTable ThongKeDoanhThuTheoNgay(DateTime tu, DateTime den) =>
            DataProvider.ExecuteQuery(
                @"SELECT CAST(GioRa AS date) AS Ngay, COUNT(*) AS SoHoaDon, SUM(TongTien) AS DoanhThu
                  FROM HoaDon
                  WHERE TrangThai = 1 AND GioRa >= @tu AND GioRa < DATEADD(DAY, 1, @den)
                  GROUP BY CAST(GioRa AS date) ORDER BY Ngay",
                new SqlParameter("@tu", SqlDbType.DateTime) { Value = tu.Date },
                new SqlParameter("@den", SqlDbType.DateTime) { Value = den.Date });

        public static DataTable ThongKeTopMon(DateTime tu, DateTime den) =>
            DataProvider.ExecuteQuery(
                @"SELECT TOP 10 m.TenMon, SUM(ct.SoLuong) AS SoLuongBan, SUM(ct.SoLuong * ct.DonGia) AS DoanhThu
                  FROM ChiTietHoaDon ct
                  JOIN HoaDon h ON h.MaHoaDon = ct.MaHoaDon
                  JOIN MonAn m ON m.MaMon = ct.MaMon
                  WHERE h.TrangThai = 1 AND h.GioRa >= @tu AND h.GioRa < DATEADD(DAY, 1, @den)
                  GROUP BY m.TenMon ORDER BY SoLuongBan DESC",
                new SqlParameter("@tu", SqlDbType.DateTime) { Value = tu.Date },
                new SqlParameter("@den", SqlDbType.DateTime) { Value = den.Date });

        /// <summary>Chi tiet mon da goi (SqlDataAdapter -> DataTable).</summary>
        public static DataTable GetChiTiet(int maHoaDon) =>
            DataProvider.ExecuteQuery(
                @"SELECT ct.MaMon, m.TenMon, ct.SoLuong, ct.DonGia, ct.SoLuong * ct.DonGia AS ThanhTien
                  FROM ChiTietHoaDon ct JOIN MonAn m ON m.MaMon = ct.MaMon
                  WHERE ct.MaHoaDon=@hd ORDER BY m.TenMon",
                new SqlParameter("@hd", maHoaDon));

        /// <summary>Them mon: tu tao hoa don neu ban chua co, doi ban sang "Co khach". Bao boi transaction.</summary>
        public static void ThemMon(int maBan, int maMon, int soLuong)
        {
            using (var conn = DataProvider.CreateConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        int maHD;
                        using (var cmd = new SqlCommand(SQL_HD_CHUA_TT, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@b", maBan);
                            object o = cmd.ExecuteScalar();
                            if (o == null)
                            {
                                cmd.CommandText =
                                    @"INSERT HoaDon(MaBan, GioVao, TongTien, TrangThai)
                                      OUTPUT INSERTED.MaHoaDon VALUES (@b, GETDATE(), 0, 0)";
                                o = cmd.ExecuteScalar();
                            }
                            maHD = Convert.ToInt32(o);
                        }

                        // Mon da co -> cong don so luong, chua co -> them dong moi (lay gia hien hanh)
                        using (var cmd = new SqlCommand(
                            @"UPDATE ChiTietHoaDon SET SoLuong = SoLuong + @sl
                              WHERE MaHoaDon=@hd AND MaMon=@m;
                              IF @@ROWCOUNT = 0
                                  INSERT ChiTietHoaDon(MaHoaDon, MaMon, SoLuong, DonGia)
                                  SELECT @hd, MaMon, @sl, DonGia FROM MonAn WHERE MaMon=@m;", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@hd", maHD);
                            cmd.Parameters.AddWithValue("@m", maMon);
                            cmd.Parameters.AddWithValue("@sl", soLuong);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new SqlCommand(
                            "UPDATE BanAn SET TrangThai=N'Có khách' WHERE MaBan=@b", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@b", maBan);
                            cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                    }
                    catch { tran.Rollback(); throw; }
                }
            }
        }

        /// <summary>Thanh toan: tinh tong -> cap nhat HoaDon -> tra ban ve "Trong" (1 SqlTransaction).</summary>
        public static decimal ThanhToan(int maHoaDon, int maBan)
        {
            using (var conn = DataProvider.CreateConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        decimal tong;
                        using (var cmd = new SqlCommand(
                            "SELECT ISNULL(SUM(SoLuong * DonGia), 0) FROM ChiTietHoaDon WHERE MaHoaDon=@hd",
                            conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@hd", maHoaDon);
                            tong = Convert.ToDecimal(cmd.ExecuteScalar());
                        }

                        using (var cmd = new SqlCommand(
                            @"UPDATE HoaDon SET GioRa = GETDATE(), TongTien = @t, TrangThai = 1
                              WHERE MaHoaDon=@hd AND TrangThai = 0", conn, tran))
                        {
                            cmd.Parameters.Add(new SqlParameter("@t", SqlDbType.Decimal)
                                { Precision = 18, Scale = 2, Value = tong });
                            cmd.Parameters.AddWithValue("@hd", maHoaDon);
                            if (cmd.ExecuteNonQuery() == 0)
                                throw new InvalidOperationException("Hóa đơn đã được thanh toán trước đó.");
                        }

                        using (var cmd = new SqlCommand(
                            "UPDATE BanAn SET TrangThai=N'Trống' WHERE MaBan=@b", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@b", maBan);
                            cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                        return tong;
                    }
                    catch { tran.Rollback(); throw; }
                }
            }
        }

        /// <summary>Doi ban: chuyen hoa don sang ban trong khac (1 SqlTransaction).</summary>
        public static void ChuyenBan(int maHoaDon, int banCu, int banMoi)
        {
            using (var conn = DataProvider.CreateConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqlCommand(
                            "SELECT TrangThai FROM BanAn WITH (UPDLOCK) WHERE MaBan=@b", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@b", banMoi);
                            string tt = cmd.ExecuteScalar() as string;
                            if (tt != "Trống")
                                throw new InvalidOperationException("Bàn đích không còn trống.");
                        }
                        using (var cmd = new SqlCommand(
                            @"UPDATE HoaDon SET MaBan=@moi WHERE MaHoaDon=@hd;
                              UPDATE BanAn SET TrangThai=N'Trống'    WHERE MaBan=@cu;
                              UPDATE BanAn SET TrangThai=N'Có khách' WHERE MaBan=@moi;", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@hd", maHoaDon);
                            cmd.Parameters.AddWithValue("@cu", banCu);
                            cmd.Parameters.AddWithValue("@moi", banMoi);
                            cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                    }
                    catch { tran.Rollback(); throw; }
                }
            }
        }
    }
}
