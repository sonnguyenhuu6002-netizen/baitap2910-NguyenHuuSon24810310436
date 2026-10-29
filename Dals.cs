using System.Data;
using HotelManagement.DTO;
using Microsoft.Data.SqlClient;

namespace HotelManagement.DAL
{
    public class PhongDAL
    {
        public DataTable GetAll() => DbHelper.GetTable(
            "SELECT SoPhong, LoaiPhong, GiaTheoGio, TrangThai FROM Phong ORDER BY SoPhong");

        public DataTable GetByStatus(string trangThai) => DbHelper.GetTable(
            "SELECT SoPhong, LoaiPhong, GiaTheoGio, TrangThai FROM Phong WHERE TrangThai = @tt ORDER BY SoPhong",
            new SqlParameter("@tt", trangThai));

        public PhongDTO GetBySoPhong(string soPhong)
        {
            var dt = DbHelper.GetTable("SELECT * FROM Phong WHERE SoPhong = @sp",
                new SqlParameter("@sp", soPhong));
            if (dt.Rows.Count == 0) return null;
            var r = dt.Rows[0];
            return new PhongDTO
            {
                SoPhong = (string)r["SoPhong"],
                LoaiPhong = (string)r["LoaiPhong"],
                GiaTheoGio = (decimal)r["GiaTheoGio"],
                TrangThai = (string)r["TrangThai"]
            };
        }

        public int UpdateStatus(string soPhong, string tuTrangThai, string denTrangThai) =>
            DbHelper.ExecuteNonQuery(
                "UPDATE Phong SET TrangThai = @new WHERE SoPhong = @sp AND TrangThai = @old",
                new SqlParameter("@new", denTrangThai),
                new SqlParameter("@sp", soPhong),
                new SqlParameter("@old", tuTrangThai));
    }

    public class KhachHangDAL
    {
        private static KhachHangDTO Map(DataRow r) => new KhachHangDTO
        {
            MaKH = (int)r["MaKH"],
            HoTen = (string)r["HoTen"],
            CCCD = (string)r["CCCD"],
            SoDienThoai = r["SoDienThoai"] == DBNull.Value ? null : (string)r["SoDienThoai"]
        };

        public KhachHangDTO GetByCCCD(string cccd)
        {
            var dt = DbHelper.GetTable("SELECT * FROM KhachHang WHERE CCCD = @cccd",
                new SqlParameter("@cccd", cccd));
            return dt.Rows.Count == 0 ? null : Map(dt.Rows[0]);
        }

        public KhachHangDTO GetById(int maKH)
        {
            var dt = DbHelper.GetTable("SELECT * FROM KhachHang WHERE MaKH = @id",
                new SqlParameter("@id", maKH));
            return dt.Rows.Count == 0 ? null : Map(dt.Rows[0]);
        }

        public int Insert(KhachHangDTO kh)
        {
            object id = DbHelper.ExecuteScalar(
                @"INSERT INTO KhachHang(HoTen, CCCD, SoDienThoai) VALUES(@ten, @cccd, @sdt);
                  SELECT CAST(SCOPE_IDENTITY() AS int);",
                new SqlParameter("@ten", kh.HoTen),
                new SqlParameter("@cccd", kh.CCCD),
                new SqlParameter("@sdt", (object)kh.SoDienThoai ?? DBNull.Value));
            return (int)id;
        }
    }

    public class DichVuDAL
    {
        public DataTable GetAll() => DbHelper.GetTable(
            @"SELECT MaDV, TenDV, DonGia,
                     TenDV + N' - ' + FORMAT(DonGia, 'N0') + N'đ' AS HienThi
              FROM DichVu ORDER BY TenDV");
    }

    public class PhieuDatPhongDAL
    {
        /// <summary>Check-in trong 1 transaction: đổi trạng thái phòng + lập phiếu. Trả 0 nếu phòng không còn trống.</summary>
        public int CheckIn(string soPhong, int maKH, DateTime ngayNhan)
        {
            using var conn = DbHelper.GetConnection();
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                using var upd = new SqlCommand(
                    "UPDATE Phong SET TrangThai = N'Đang ở' WHERE SoPhong = @sp AND TrangThai = N'Trống'",
                    conn, tran);
                upd.Parameters.AddWithValue("@sp", soPhong);
                if (upd.ExecuteNonQuery() == 0) { tran.Rollback(); return 0; }

                using var ins = new SqlCommand(
                    @"INSERT INTO PhieuDatPhong(SoPhong, MaKH, NgayNhanPhong) VALUES(@sp, @kh, @nn);
                      SELECT CAST(SCOPE_IDENTITY() AS int);", conn, tran);
                ins.Parameters.AddWithValue("@sp", soPhong);
                ins.Parameters.AddWithValue("@kh", maKH);
                ins.Parameters.AddWithValue("@nn", ngayNhan);
                int id = (int)ins.ExecuteScalar();
                tran.Commit();
                return id;
            }
            catch { tran.Rollback(); throw; }
        }

        /// <summary>Phiếu đang mở (chưa trả phòng) của một phòng.</summary>
        public PhieuDatPhongDTO GetActiveByRoom(string soPhong)
        {
            var dt = DbHelper.GetTable(
                @"SELECT TOP 1 MaPhieu, SoPhong, MaKH, NgayNhanPhong
                  FROM PhieuDatPhong WHERE SoPhong = @sp AND NgayTraPhong IS NULL
                  ORDER BY MaPhieu DESC",
                new SqlParameter("@sp", soPhong));
            if (dt.Rows.Count == 0) return null;
            var r = dt.Rows[0];
            return new PhieuDatPhongDTO
            {
                MaPhieu = (int)r["MaPhieu"],
                SoPhong = (string)r["SoPhong"],
                MaKH = (int)r["MaKH"],
                NgayNhanPhong = (DateTime)r["NgayNhanPhong"]
            };
        }

        public int AddService(SuDungDichVuDTO sd) => DbHelper.ExecuteNonQuery(
            "INSERT INTO SuDungDichVu(MaPhieu, MaDV, SoLuong, ThoiGian) VALUES(@mp, @dv, @sl, @tg)",
            new SqlParameter("@mp", sd.MaPhieu),
            new SqlParameter("@dv", sd.MaDV),
            new SqlParameter("@sl", sd.SoLuong),
            new SqlParameter("@tg", sd.ThoiGian));

        public DataTable GetServices(int maPhieu) => DbHelper.GetTable(
            @"SELECT d.TenDV AS [Dịch vụ], s.SoLuong AS [Số lượng], d.DonGia AS [Đơn giá],
                     s.SoLuong * d.DonGia AS [Thành tiền], s.ThoiGian AS [Thời gian]
              FROM SuDungDichVu s JOIN DichVu d ON d.MaDV = s.MaDV
              WHERE s.MaPhieu = @mp ORDER BY s.ThoiGian",
            new SqlParameter("@mp", maPhieu));

        public decimal GetServiceTotal(int maPhieu)
        {
            object o = DbHelper.ExecuteScalar(
                @"SELECT ISNULL(SUM(s.SoLuong * d.DonGia), 0)
                  FROM SuDungDichVu s JOIN DichVu d ON d.MaDV = s.MaDV WHERE s.MaPhieu = @mp",
                new SqlParameter("@mp", maPhieu));
            return Convert.ToDecimal(o);
        }

        /// <summary>Check-out trong 1 SqlTransaction: lưu tổng tiền + ngày trả, phòng -> Đang dọn dẹp.</summary>
        public void CheckOut(int maPhieu, string soPhong, DateTime ngayTra, decimal tongTien)
        {
            using var conn = DbHelper.GetConnection();
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                using var c1 = new SqlCommand(
                    @"UPDATE PhieuDatPhong SET NgayTraPhong = @nt, TongTienThanhToan = @tong
                      WHERE MaPhieu = @mp AND NgayTraPhong IS NULL", conn, tran);
                c1.Parameters.AddWithValue("@nt", ngayTra);
                c1.Parameters.AddWithValue("@tong", tongTien);
                c1.Parameters.AddWithValue("@mp", maPhieu);
                if (c1.ExecuteNonQuery() == 0)
                    throw new InvalidOperationException("Phiếu đã được thanh toán hoặc không tồn tại.");

                using var c2 = new SqlCommand(
                    "UPDATE Phong SET TrangThai = N'Đang dọn dẹp' WHERE SoPhong = @sp", conn, tran);
                c2.Parameters.AddWithValue("@sp", soPhong);
                c2.ExecuteNonQuery();

                tran.Commit();
            }
            catch { tran.Rollback(); throw; }
        }
    }
}
