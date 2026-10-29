using System.Data;
using InventoryManagement.DTO;
using Microsoft.Data.SqlClient;

namespace InventoryManagement.DAL
{
    public class NhaCungCapDAL
    {
        public DataTable GetAll() => DbHelper.GetTable(
            "SELECT MaNCC, TenNCC, DienThoai, DiaChi FROM NhaCungCap ORDER BY TenNCC");

        public DataTable Search(string keyword) => DbHelper.GetTable(
            @"SELECT MaNCC, TenNCC, DienThoai, DiaChi FROM NhaCungCap
              WHERE TenNCC LIKE @kw OR DienThoai LIKE @kw ORDER BY TenNCC",
            new SqlParameter("@kw", "%" + keyword + "%"));

        public int Insert(NhaCungCapDTO n) => (int)DbHelper.ExecuteScalar(
            @"INSERT INTO NhaCungCap(TenNCC, DienThoai, DiaChi) VALUES(@ten, @dt, @dc);
              SELECT CAST(SCOPE_IDENTITY() AS int);",
            new SqlParameter("@ten", n.TenNCC),
            new SqlParameter("@dt", (object)n.DienThoai ?? DBNull.Value),
            new SqlParameter("@dc", (object)n.DiaChi ?? DBNull.Value));

        public int Update(NhaCungCapDTO n) => DbHelper.ExecuteNonQuery(
            "UPDATE NhaCungCap SET TenNCC=@ten, DienThoai=@dt, DiaChi=@dc WHERE MaNCC=@ma",
            new SqlParameter("@ten", n.TenNCC),
            new SqlParameter("@dt", (object)n.DienThoai ?? DBNull.Value),
            new SqlParameter("@dc", (object)n.DiaChi ?? DBNull.Value),
            new SqlParameter("@ma", n.MaNCC));

        public int Delete(int maNCC) => DbHelper.ExecuteNonQuery(
            "DELETE FROM NhaCungCap WHERE MaNCC=@ma", new SqlParameter("@ma", maNCC));
    }

    public class VatTuDAL
    {
        public DataTable GetAll() => DbHelper.GetTable(
            "SELECT MaVatTu, TenVatTu, DVT, SoLuongTon FROM VatTu ORDER BY MaVatTu");

        public DataTable Search(string keyword) => DbHelper.GetTable(
            @"SELECT MaVatTu, TenVatTu, DVT, SoLuongTon FROM VatTu
              WHERE MaVatTu LIKE @kw OR TenVatTu LIKE @kw ORDER BY MaVatTu",
            new SqlParameter("@kw", "%" + keyword + "%"));

        public DataTable GetForCombo() => DbHelper.GetTable(
            @"SELECT MaVatTu, TenVatTu,
                     MaVatTu + N' - ' + TenVatTu + N' (tồn: ' + CAST(SoLuongTon AS nvarchar(30)) + N' ' + ISNULL(DVT, N'') + N')' AS HienThi
              FROM VatTu ORDER BY MaVatTu");

        public int Insert(VatTuDTO v) => DbHelper.ExecuteNonQuery(
            "INSERT INTO VatTu(MaVatTu, TenVatTu, DVT) VALUES(@ma, @ten, @dvt)",
            new SqlParameter("@ma", v.MaVatTu),
            new SqlParameter("@ten", v.TenVatTu),
            new SqlParameter("@dvt", (object)v.DVT ?? DBNull.Value));

        // Không cập nhật SoLuongTon: tồn kho chỉ thay đổi qua phiếu nhập/xuất
        public int Update(VatTuDTO v) => DbHelper.ExecuteNonQuery(
            "UPDATE VatTu SET TenVatTu=@ten, DVT=@dvt WHERE MaVatTu=@ma",
            new SqlParameter("@ten", v.TenVatTu),
            new SqlParameter("@dvt", (object)v.DVT ?? DBNull.Value),
            new SqlParameter("@ma", v.MaVatTu));

        public int Delete(string maVatTu) => DbHelper.ExecuteNonQuery(
            "DELETE FROM VatTu WHERE MaVatTu=@ma", new SqlParameter("@ma", maVatTu));

        public DataTable GetTonThap(decimal nguong) => DbHelper.GetTable(
            @"SELECT MaVatTu, TenVatTu, DVT, SoLuongTon FROM VatTu
              WHERE SoLuongTon < @ng ORDER BY SoLuongTon",
            new SqlParameter("@ng", nguong));

        /// <summary>ExecuteScalar: tổng giá trị tồn = tồn x đơn giá nhập gần nhất của từng vật tư.</summary>
        public decimal GetTongGiaTriTon()
        {
            object o = DbHelper.ExecuteScalar(
                @"SELECT ISNULL(SUM(v.SoLuongTon * ISNULL(g.DonGia, 0)), 0)
                  FROM VatTu v
                  OUTER APPLY (
                      SELECT TOP 1 ct.DonGia
                      FROM ChiTietPhieuKho ct JOIN PhieuKho p ON p.MaPhieu = ct.MaPhieu
                      WHERE ct.MaVatTu = v.MaVatTu AND p.LoaiPhieu = N'NHAP'
                      ORDER BY p.NgayLap DESC, p.MaPhieu DESC) g");
            return Convert.ToDecimal(o);
        }
    }

    public class PhieuKhoDAL
    {
        /// <summary>
        /// Lưu phiếu kho trong 1 SqlTransaction (ACID): phiếu + chi tiết + cập nhật tồn kho.
        /// Có lỗi ở bất kỳ bước nào thì Rollback toàn bộ.
        /// </summary>
        public int LuuPhieu(PhieuKhoDTO phieu, List<ChiTietPhieuKhoDTO> ds)
        {
            using var conn = DbHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // 1. Chèn PhieuKho
                int maPhieu;
                using (var cmd = new SqlCommand(
                    @"INSERT INTO PhieuKho(LoaiPhieu, NgayLap, MaNCC, TongTien) VALUES(@loai, @ngay, @ncc, @tong);
                      SELECT CAST(SCOPE_IDENTITY() AS int);", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@loai", phieu.LoaiPhieu);
                    cmd.Parameters.AddWithValue("@ngay", phieu.NgayLap);
                    cmd.Parameters.AddWithValue("@ncc", (object)phieu.MaNCC ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@tong", phieu.TongTien);
                    maPhieu = (int)cmd.ExecuteScalar();
                }

                foreach (var ct in ds)
                {
                    // Phiếu xuất: khóa dòng và kiểm tra tồn kho trước
                    if (phieu.LoaiPhieu == "XUAT")
                    {
                        using var chk = new SqlCommand(
                            "SELECT SoLuongTon FROM VatTu WITH (UPDLOCK, ROWLOCK) WHERE MaVatTu = @ma", conn, trans);
                        chk.Parameters.AddWithValue("@ma", ct.MaVatTu);
                        object ton = chk.ExecuteScalar();
                        if (ton == null)
                            throw new InvalidOperationException($"Vật tư {ct.MaVatTu} không tồn tại.");
                        if ((decimal)ton < ct.SoLuong)   // thiếu hàng -> lỗi -> catch gọi trans.Rollback()
                            throw new InvalidOperationException(
                                $"Không đủ hàng trong kho (vật tư {ct.MaVatTu}: tồn {(decimal)ton:N2}, cần xuất {ct.SoLuong:N2}).");
                    }

                    // 2. Chèn ChiTietPhieuKho
                    using (var ins = new SqlCommand(
                        "INSERT INTO ChiTietPhieuKho(MaPhieu, MaVatTu, SoLuong, DonGia) VALUES(@mp, @ma, @sl, @dg)",
                        conn, trans))
                    {
                        ins.Parameters.AddWithValue("@mp", maPhieu);
                        ins.Parameters.AddWithValue("@ma", ct.MaVatTu);
                        ins.Parameters.AddWithValue("@sl", ct.SoLuong);
                        ins.Parameters.AddWithValue("@dg", ct.DonGia);
                        ins.ExecuteNonQuery();
                    }

                    // 3. Cập nhật tồn kho: NHAP cộng, XUAT trừ
                    decimal delta = phieu.LoaiPhieu == "NHAP" ? ct.SoLuong : -ct.SoLuong;
                    using (var upd = new SqlCommand(
                        "UPDATE VatTu SET SoLuongTon = SoLuongTon + @delta WHERE MaVatTu = @ma", conn, trans))
                    {
                        upd.Parameters.AddWithValue("@delta", delta);
                        upd.Parameters.AddWithValue("@ma", ct.MaVatTu);
                        if (upd.ExecuteNonQuery() == 0)
                            throw new InvalidOperationException($"Vật tư {ct.MaVatTu} không tồn tại.");
                    }
                }

                // 4. Mọi bước thành công -> Commit
                trans.Commit();
                return maPhieu;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                if (ex is SqlException sql) throw DbHelper.Translate(sql);
                throw;
            }
        }

        public DataTable GetDanhSach() => DbHelper.GetTable(
            @"SELECT p.MaPhieu, p.LoaiPhieu, p.NgayLap, n.TenNCC, p.TongTien
              FROM PhieuKho p LEFT JOIN NhaCungCap n ON n.MaNCC = p.MaNCC
              ORDER BY p.MaPhieu DESC");

        public DataTable GetChiTiet(int maPhieu) => DbHelper.GetTable(
            @"SELECT c.MaVatTu, v.TenVatTu, c.SoLuong, c.DonGia, c.SoLuong * c.DonGia AS ThanhTien
              FROM ChiTietPhieuKho c JOIN VatTu v ON v.MaVatTu = c.MaVatTu
              WHERE c.MaPhieu = @mp",
            new SqlParameter("@mp", maPhieu));
    }
}
