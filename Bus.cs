using System.Data;
using HotelManagement.DAL;
using HotelManagement.DTO;

namespace HotelManagement.BUS
{
    public class PhongBUS
    {
        private readonly PhongDAL _dal = new();
        public DataTable GetAll() => _dal.GetAll();
        public DataTable GetByStatus(string tt) => _dal.GetByStatus(tt);

        public void DonXong(string soPhong)
        {
            if (string.IsNullOrEmpty(soPhong)) throw new ArgumentException("Hãy chọn một phòng.");
            if (_dal.UpdateStatus(soPhong, "Đang dọn dẹp", "Trống") == 0)
                throw new InvalidOperationException("Phòng này không ở trạng thái 'Đang dọn dẹp'.");
        }
    }

    public class KhachHangBUS
    {
        private readonly KhachHangDAL _dal = new();
        public KhachHangDTO TimTheoCCCD(string cccd)
        {
            if (string.IsNullOrWhiteSpace(cccd)) throw new ArgumentException("Vui lòng nhập CCCD.");
            return _dal.GetByCCCD(cccd.Trim());
        }
    }

    public class DichVuBUS
    {
        private readonly DichVuDAL _dal = new();
        public DataTable GetAll() => _dal.GetAll();
    }

    public class PhieuBUS
    {
        private readonly PhieuDatPhongDAL _dal = new();
        private readonly KhachHangDAL _khDal = new();
        private readonly PhongDAL _phongDal = new();

        // ---------- Check-in ----------
        public int CheckIn(string cccd, string hoTen, string sdt, string soPhong)
        {
            if (string.IsNullOrWhiteSpace(cccd)) throw new ArgumentException("Vui lòng nhập CCCD.");
            if (string.IsNullOrEmpty(soPhong)) throw new ArgumentException("Vui lòng chọn phòng trống.");
            cccd = cccd.Trim();

            var kh = _khDal.GetByCCCD(cccd);
            if (kh == null) // khách mới -> thêm
            {
                if (string.IsNullOrWhiteSpace(hoTen))
                    throw new ArgumentException("Khách mới: vui lòng nhập họ tên.");
                kh = new KhachHangDTO { HoTen = hoTen.Trim(), CCCD = cccd, SoDienThoai = sdt?.Trim() };
                kh.MaKH = _khDal.Insert(kh);
            }

            int maPhieu = _dal.CheckIn(soPhong, kh.MaKH, DateTime.Now);
            if (maPhieu == 0) throw new InvalidOperationException("Phòng không còn trống.");
            return maPhieu;
        }

        // ---------- Gọi dịch vụ ----------
        public void GoiDichVu(string soPhong, int maDV, int soLuong)
        {
            if (soLuong <= 0) throw new ArgumentException("Số lượng phải > 0.");
            var phieu = _dal.GetActiveByRoom(soPhong)
                        ?? throw new InvalidOperationException("Phòng chưa có khách đang ở.");
            _dal.AddService(new SuDungDichVuDTO
            {
                MaPhieu = phieu.MaPhieu, MaDV = maDV, SoLuong = soLuong, ThoiGian = DateTime.Now
            });
        }

        public DataTable GetDichVuDaDung(string soPhong)
        {
            var phieu = _dal.GetActiveByRoom(soPhong);
            return phieu == null ? new DataTable() : _dal.GetServices(phieu.MaPhieu);
        }

        // ---------- Check-out ----------
        /// <summary>Tính hóa đơn = (số giờ làm tròn lên, tối thiểu 1) x giá giờ + tiền dịch vụ.</summary>
        public HoaDonDTO TinhHoaDon(string soPhong)
        {
            if (string.IsNullOrEmpty(soPhong)) throw new ArgumentException("Hãy chọn phòng.");
            var phieu = _dal.GetActiveByRoom(soPhong)
                        ?? throw new InvalidOperationException("Phòng chưa có khách đang ở.");
            var phong = _phongDal.GetBySoPhong(soPhong);
            var kh = _khDal.GetById(phieu.MaKH);

            DateTime ngayTra = DateTime.Now;
            int soGio = (int)Math.Max(1, Math.Ceiling((ngayTra - phieu.NgayNhanPhong).TotalHours));
            decimal tienPhong = soGio * phong.GiaTheoGio;
            decimal tienDV = _dal.GetServiceTotal(phieu.MaPhieu);

            return new HoaDonDTO
            {
                MaPhieu = phieu.MaPhieu, SoPhong = soPhong, HoTen = kh?.HoTen,
                NgayNhan = phieu.NgayNhanPhong, NgayTra = ngayTra, SoGio = soGio,
                GiaTheoGio = phong.GiaTheoGio, TienPhong = tienPhong,
                TienDichVu = tienDV, TongTien = tienPhong + tienDV
            };
        }

        /// <summary>Tính lại hóa đơn tại thời điểm xác nhận rồi ghi DB bằng SqlTransaction.</summary>
        public HoaDonDTO ThanhToan(string soPhong)
        {
            var hd = TinhHoaDon(soPhong);
            _dal.CheckOut(hd.MaPhieu, hd.SoPhong, hd.NgayTra, hd.TongTien);
            return hd;
        }
    }
}
