using System.Data;
using HotelManagement.BUS;
using HotelManagement.DTO;

namespace HotelManagement.GUI
{
    public class MainForm : Form
    {
        private readonly PhongBUS _phongBus = new();
        private readonly KhachHangBUS _khBus = new();
        private readonly DichVuBUS _dvBus = new();
        private readonly PhieuBUS _phieuBus = new();

        private DataView _viewPhong;
        private bool _loading;

        private DataGridView gridPhong, gridDV;
        private ComboBox cbLoai, cbTrangThai, cbPhongTrong, cbPhongO, cbDV, cbPhongCO;
        private TextBox txtCCCD, txtHoTen, txtSDT, txtHoaDon;
        private NumericUpDown nudSL;
        private Label lblKH;

        public MainForm()
        {
            Text = "Quản lý khách sạn - HotelDB";
            Size = new Size(920, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BuildUi();
            Load += (s, e) => SafeRun(ReloadAll);
        }

        // ================= Helpers dựng UI =================
        private static Label L(string text, int x, int y) =>
            new Label { Text = text, Left = x, Top = y + 3, AutoSize = true };

        private static TextBox T(int x, int y, int w) =>
            new TextBox { Left = x, Top = y, Width = w };

        private static ComboBox C(int x, int y, int w) =>
            new ComboBox { Left = x, Top = y, Width = w, DropDownStyle = ComboBoxStyle.DropDownList };

        private static Button B(string text, int x, int y, int w, EventHandler h)
        {
            var b = new Button { Text = text, Left = x, Top = y, Width = w, Height = 28 };
            b.Click += h;
            return b;
        }

        private static DataGridView G(int x, int y, int w, int h) => new DataGridView
        {
            Left = x, Top = y, Width = w, Height = h,
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        private static string Money(decimal v) => v.ToString("N0") + " đ";
        private static string Esc(string s) => s.Replace("'", "''");

        private void SafeRun(Action a)
        {
            try { a(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private static void Bind(ComboBox cb, string display, string value)
        {
            cb.DisplayMember = display;
            cb.ValueMember = value;
        }

        // ================= Dựng giao diện =================
        private void BuildUi()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildTabPhong());
            tabs.TabPages.Add(BuildTabCheckIn());
            tabs.TabPages.Add(BuildTabDichVu());
            tabs.TabPages.Add(BuildTabCheckOut());
            tabs.SelectedIndexChanged += (s, e) => SafeRun(ReloadAll);
            Controls.Add(tabs);
        }

        private TabPage BuildTabPhong()
        {
            var t = new TabPage("Danh sách phòng");
            cbLoai = C(90, 12, 120);
            cbTrangThai = C(310, 12, 130);
            cbTrangThai.Items.AddRange(new object[] { "Tất cả", "Trống", "Đang ở", "Đang dọn dẹp" });
            cbTrangThai.SelectedIndex = 0;
            cbLoai.SelectedIndexChanged += (s, e) => ApplyFilter();
            cbTrangThai.SelectedIndexChanged += (s, e) => ApplyFilter();

            gridPhong = G(10, 50, 880, 490);
            t.Controls.AddRange(new Control[]
            {
                L("Loại phòng:", 10, 12), cbLoai, L("Trạng thái:", 230, 12), cbTrangThai,
                B("Đã dọn xong → Trống", 470, 10, 170, (s, e) => SafeRun(() =>
                {
                    string so = gridPhong.CurrentRow?.Cells["SoPhong"].Value as string;
                    _phongBus.DonXong(so);
                    ReloadAll();
                })),
                gridPhong
            });
            return t;
        }

        private TabPage BuildTabCheckIn()
        {
            var t = new TabPage("Check-in");
            txtCCCD = T(110, 15, 200);
            txtHoTen = T(110, 85, 250);
            txtSDT = T(110, 120, 250);
            lblKH = new Label { Left = 110, Top = 52, Width = 600, ForeColor = Color.DarkBlue };
            cbPhongTrong = C(110, 155, 150);
            Bind(cbPhongTrong, "SoPhong", "SoPhong");

            t.Controls.AddRange(new Control[]
            {
                L("CCCD:", 20, 15), txtCCCD,
                B("Tra cứu", 320, 13, 90, (s, e) => SafeRun(TraCuuKhach)),
                lblKH,
                L("Họ tên:", 20, 85), txtHoTen,
                L("Số điện thoại:", 20, 120), txtSDT,
                L("Phòng trống:", 20, 155), cbPhongTrong,
                B("Nhận phòng (Check-in)", 110, 200, 180, (s, e) => SafeRun(DoCheckIn))
            });
            return t;
        }

        private TabPage BuildTabDichVu()
        {
            var t = new TabPage("Gọi dịch vụ");
            cbPhongO = C(70, 12, 110);
            Bind(cbPhongO, "SoPhong", "SoPhong");
            cbDV = C(270, 12, 230);
            Bind(cbDV, "HienThi", "MaDV");
            nudSL = new NumericUpDown { Left = 550, Top = 12, Width = 60, Minimum = 1, Maximum = 100, Value = 1 };
            gridDV = G(10, 55, 880, 480);

            cbPhongO.SelectedIndexChanged += (s, e) => SafeRun(LoadDichVuDaDung);

            t.Controls.AddRange(new Control[]
            {
                L("Phòng:", 10, 12), cbPhongO, L("Dịch vụ:", 200, 12), cbDV, L("SL:", 515, 12), nudSL,
                B("Thêm dịch vụ", 630, 10, 120, (s, e) => SafeRun(() =>
                {
                    string so = cbPhongO.SelectedValue as string;
                    if (so == null || cbDV.SelectedValue == null)
                        throw new InvalidOperationException("Hãy chọn phòng đang ở và dịch vụ.");
                    _phieuBus.GoiDichVu(so, Convert.ToInt32(cbDV.SelectedValue), (int)nudSL.Value);
                    LoadDichVuDaDung();
                })),
                gridDV
            });
            return t;
        }

        private TabPage BuildTabCheckOut()
        {
            var t = new TabPage("Check-out & Hóa đơn");
            cbPhongCO = C(70, 12, 110);
            Bind(cbPhongCO, "SoPhong", "SoPhong");
            txtHoaDon = new TextBox
            {
                Left = 10, Top = 55, Width = 880, Height = 480, Multiline = true, ReadOnly = true,
                Font = new Font("Consolas", 11), ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            t.Controls.AddRange(new Control[]
            {
                L("Phòng:", 10, 12), cbPhongCO,
                B("Xem hóa đơn", 200, 10, 120, (s, e) => SafeRun(() =>
                {
                    txtHoaDon.Text = FormatHoaDon(_phieuBus.TinhHoaDon(cbPhongCO.SelectedValue as string), false);
                })),
                B("Xác nhận thanh toán", 335, 10, 160, (s, e) => SafeRun(DoThanhToan)),
                txtHoaDon
            });
            return t;
        }

        // ================= Nghiệp vụ =================
        private void ReloadAll()
        {
            _loading = true;
            try
            {
                // Tab phòng: nạp 1 lần vào DataTable, lọc ngắt kết nối bằng DataView
                var dt = _phongBus.GetAll();
                string loaiCu = cbLoai.SelectedItem as string;
                cbLoai.Items.Clear();
                cbLoai.Items.Add("Tất cả");
                foreach (var l in dt.AsEnumerable().Select(r => r.Field<string>("LoaiPhong")).Distinct())
                    cbLoai.Items.Add(l);
                int idx = loaiCu == null ? 0 : cbLoai.Items.IndexOf(loaiCu);
                cbLoai.SelectedIndex = idx < 0 ? 0 : idx;

                _viewPhong = new DataView(dt);
                gridPhong.DataSource = _viewPhong;
            }
            finally { _loading = false; }
            ApplyFilter();

            cbPhongTrong.DataSource = _phongBus.GetByStatus("Trống");
            cbPhongO.DataSource = _phongBus.GetByStatus("Đang ở");
            cbPhongCO.DataSource = _phongBus.GetByStatus("Đang ở");
            cbDV.DataSource = _dvBus.GetAll();
            LoadDichVuDaDung();
        }

        private void ApplyFilter()
        {
            if (_loading || _viewPhong == null) return;
            var f = new List<string>();
            if (cbLoai.SelectedIndex > 0) f.Add($"LoaiPhong = '{Esc(cbLoai.Text)}'");
            if (cbTrangThai.SelectedIndex > 0) f.Add($"TrangThai = '{Esc(cbTrangThai.Text)}'");
            _viewPhong.RowFilter = string.Join(" AND ", f);
        }

        private void TraCuuKhach()
        {
            var kh = _khBus.TimTheoCCCD(txtCCCD.Text);
            if (kh != null)
            {
                txtHoTen.Text = kh.HoTen;
                txtSDT.Text = kh.SoDienThoai;
                txtHoTen.ReadOnly = txtSDT.ReadOnly = true;
                lblKH.Text = "Khách cũ - đã có trong hệ thống.";
            }
            else
            {
                txtHoTen.Clear(); txtSDT.Clear();
                txtHoTen.ReadOnly = txtSDT.ReadOnly = false;
                lblKH.Text = "Khách mới - vui lòng nhập họ tên và số điện thoại.";
            }
        }

        private void DoCheckIn()
        {
            int maPhieu = _phieuBus.CheckIn(txtCCCD.Text, txtHoTen.Text, txtSDT.Text,
                                            cbPhongTrong.SelectedValue as string);
            MessageBox.Show($"Check-in thành công. Mã phiếu: {maPhieu}", "Thành công");
            txtCCCD.Clear(); txtHoTen.Clear(); txtSDT.Clear(); lblKH.Text = "";
            txtHoTen.ReadOnly = txtSDT.ReadOnly = false;
            ReloadAll();
        }

        private void LoadDichVuDaDung()
        {
            string so = cbPhongO.SelectedValue as string;
            gridDV.DataSource = so == null ? null : _phieuBus.GetDichVuDaDung(so);
            if (gridDV.Columns.Contains("Đơn giá")) gridDV.Columns["Đơn giá"].DefaultCellStyle.Format = "N0";
            if (gridDV.Columns.Contains("Thành tiền")) gridDV.Columns["Thành tiền"].DefaultCellStyle.Format = "N0";
        }

        private void DoThanhToan()
        {
            string so = cbPhongCO.SelectedValue as string;
            var preview = _phieuBus.TinhHoaDon(so);
            var ok = MessageBox.Show($"Tổng cần thanh toán: {Money(preview.TongTien)}\nXác nhận thanh toán và trả phòng {so}?",
                                     "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ok != DialogResult.Yes) return;

            var hd = _phieuBus.ThanhToan(so);          // SqlTransaction nằm trong DAL
            ReloadAll();
            txtHoaDon.Text = FormatHoaDon(hd, true);
        }

        private static string FormatHoaDon(HoaDonDTO h, bool daThanhToan)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(daThanhToan ? "========= HÓA ĐƠN (ĐÃ THANH TOÁN) =========" : "========= HÓA ĐƠN TẠM TÍNH =========");
            sb.AppendLine($"Mã phiếu    : {h.MaPhieu}");
            sb.AppendLine($"Phòng       : {h.SoPhong}");
            sb.AppendLine($"Khách hàng  : {h.HoTen}");
            sb.AppendLine($"Nhận phòng  : {h.NgayNhan:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"Trả phòng   : {h.NgayTra:dd/MM/yyyy HH:mm}");
            sb.AppendLine("--------------------------------------------");
            sb.AppendLine($"Số giờ      : {h.SoGio} (làm tròn lên)");
            sb.AppendLine($"Giá theo giờ: {Money(h.GiaTheoGio)}");
            sb.AppendLine($"Tiền phòng  : {Money(h.TienPhong)}");
            sb.AppendLine($"Tiền dịch vụ: {Money(h.TienDichVu)}");
            sb.AppendLine("--------------------------------------------");
            sb.AppendLine($"TỔNG CỘNG   : {Money(h.TongTien)}");
            return sb.ToString();
        }
    }
}
