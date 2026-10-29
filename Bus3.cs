using System.Data;
using InventoryManagement.DAL;
using InventoryManagement.DTO;

namespace InventoryManagement.BUS
{
    public class NhaCungCapBUS
    {
        private readonly NhaCungCapDAL _dal = new();

        public DataTable GetAll() => _dal.GetAll();

        public DataTable TimKiem(string kw) =>
            string.IsNullOrWhiteSpace(kw) ? _dal.GetAll() : _dal.Search(kw.Trim());

        private static void Validate(NhaCungCapDTO n)
        {
            if (string.IsNullOrWhiteSpace(n.TenNCC)) throw new ArgumentException("Tên nhà cung cấp không được trống.");
            n.TenNCC = n.TenNCC.Trim();
            n.DienThoai = string.IsNullOrWhiteSpace(n.DienThoai) ? null : n.DienThoai.Trim();
            n.DiaChi = string.IsNullOrWhiteSpace(n.DiaChi) ? null : n.DiaChi.Trim();
        }

        public int Them(NhaCungCapDTO n) { Validate(n); return _dal.Insert(n); }

        public void Sua(NhaCungCapDTO n)
        {
            Validate(n);
            if (_dal.Update(n) == 0) throw new InvalidOperationException("Không tìm thấy nhà cung cấp cần sửa.");
        }

        public void Xoa(int maNCC)
        {
            if (_dal.Delete(maNCC) == 0) throw new InvalidOperationException("Không tìm thấy nhà cung cấp cần xóa.");
        }
    }

    public class VatTuBUS
    {
        private readonly VatTuDAL _dal = new();

        public DataTable GetAll() => _dal.GetAll();
        public DataTable GetForCombo() => _dal.GetForCombo();

        public DataTable TimKiem(string kw) =>
            string.IsNullOrWhiteSpace(kw) ? _dal.GetAll() : _dal.Search(kw.Trim());

        private static void Validate(VatTuDTO v)
        {
            if (string.IsNullOrWhiteSpace(v.MaVatTu)) throw new ArgumentException("Mã vật tư không được trống.");
            if (string.IsNullOrWhiteSpace(v.TenVatTu)) throw new ArgumentException("Tên vật tư không được trống.");
            v.MaVatTu = v.MaVatTu.Trim();
            v.TenVatTu = v.TenVatTu.Trim();
            v.DVT = string.IsNullOrWhiteSpace(v.DVT) ? null : v.DVT.Trim();
        }

        public void Them(VatTuDTO v) { Validate(v); _dal.Insert(v); }

        public void Sua(VatTuDTO v)
        {
            Validate(v);
            if (_dal.Update(v) == 0) throw new InvalidOperationException("Không tìm thấy vật tư cần sửa (không được đổi mã).");
        }

        public void Xoa(string ma)
        {
            if (string.IsNullOrWhiteSpace(ma)) throw new ArgumentException("Hãy chọn vật tư cần xóa.");
            if (_dal.Delete(ma.Trim()) == 0) throw new InvalidOperationException("Không tìm thấy vật tư cần xóa.");
        }

        public DataTable GetTonThap(decimal nguong)
        {
            if (nguong < 0) throw new ArgumentException("Ngưỡng phải >= 0.");
            return _dal.GetTonThap(nguong);
        }

        public decimal TinhTongGiaTriTon() => _dal.GetTongGiaTriTon();
    }

    public class PhieuKhoBUS
    {
        private readonly PhieuKhoDAL _dal = new();

        public int LuuPhieu(string loaiPhieu, int? maNCC, List<ChiTietPhieuKhoDTO> ds)
        {
            if (loaiPhieu != "NHAP" && loaiPhieu != "XUAT")
                throw new ArgumentException("Loại phiếu phải là NHAP hoặc XUAT.");
            if (ds == null || ds.Count == 0)
                throw new ArgumentException("Phiếu chưa có vật tư nào.");
            if (loaiPhieu == "NHAP" && maNCC == null)
                throw new ArgumentException("Phiếu nhập phải chọn nhà cung cấp.");
            if (ds.GroupBy(x => x.MaVatTu).Any(g => g.Count() > 1))
                throw new ArgumentException("Có vật tư bị lặp trong phiếu.");
            foreach (var ct in ds)
            {
                if (ct.SoLuong <= 0) throw new ArgumentException($"Số lượng của {ct.MaVatTu} phải > 0.");
                if (ct.DonGia < 0) throw new ArgumentException($"Đơn giá của {ct.MaVatTu} không được âm.");
            }

            var phieu = new PhieuKhoDTO
            {
                LoaiPhieu = loaiPhieu,
                NgayLap = DateTime.Now,
                MaNCC = loaiPhieu == "NHAP" ? maNCC : null,   // phiếu xuất không có NCC
                TongTien = ds.Sum(x => x.SoLuong * x.DonGia)
            };
            return _dal.LuuPhieu(phieu, ds);
        }

        public DataTable GetDanhSachPhieu() => _dal.GetDanhSach();
        public DataTable GetChiTiet(int maPhieu) => _dal.GetChiTiet(maPhieu);
    }
}
