using System.Data;
using InventoryManagement.BUS;
using InventoryManagement.DTO;

namespace InventoryManagement.GUI
{
    public class MainForm : Form
    {
        private readonly VatTuBUS _vtBus = new();
        private readonly NhaCungCapBUS _nccBus = new();
        private readonly PhieuKhoBUS _phieuBus = new();

        // Tab vật tư
        private TextBox txtTimVT, txtMaVT, txtTenVT, txtDVT, txtTonVT;
        private DataGridView gridVT;
        // Tab nhà cung cấp
        private TextBox txtTimNCC, txtMaNCC, txtTenNCC, txtSDTNCC, txtDiaChiNCC;
        private DataGridView gridNCC;
        // Tab lập phiếu
        private ComboBox cbLoai, cbNCC, cbVT;
        private NumericUpDown nudSL, nudGia;
        private DataGridView gridCT;
        private Label lblTong;
        private DataTable _dtChiTiet;
        // Tab cảnh báo
        private NumericUpDown nudNguong;
        private DataGridView gridCB;
        private Label lblCB, lblTongGT;
        // Tab lịch sử
        private DataGridView gridPhieu, gridLS;

        public MainForm()
        {
            Text = "Quản lý xuất - nhập kho nguyên vật liệu";
            Size = new Size(960, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BuildUi();
            Load += (s, e) => SafeRun(ReloadAll);
        }

        // ================= Helpers =================
        private static Label L(string text, int x, int y) =>
            new Label { Text = text, Left = x, Top = y + 3, AutoSize = true };

        private static TextBox T(int x, int y, int w, bool ro = false) =>
            new TextBox { Left = x, Top = y, Width = w, ReadOnly = ro };

        private static ComboBox C(int x, int y, int w) =>
            new ComboBox { Left = x, Top = y, Width = w, DropDownStyle = ComboBoxStyle.DropDownList };

        private static NumericUpDown N(int x, int y, int w, decimal max) => new NumericUpDown
        {
            Left = x, Top = y, Width = w, Maximum = max, DecimalPlaces = 2, ThousandsSeparator = true
        };

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
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        private static string Money(decimal v) => v.ToString("N0") + " đ";
        private static string Esc(string s) => s.Replace("'", "''");
        private static string Cell(DataGridViewRow r, string col) => Convert.ToString(r.Cells[col].Value);

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
            tabs.TabPages.Add(BuildTabVatTu());
            tabs.TabPages.Add(BuildTabNCC());
            tabs.TabPages.Add(BuildTabPhieu());
            tabs.TabPages.Add(BuildTabCanhBao());
            tabs.TabPages.Add(BuildTabLichSu());
            tabs.SelectedIndexChanged += (s, e) => SafeRun(ReloadAll);
            Controls.Add(tabs);
        }

        private TabPage BuildTabVatTu()
        {
            var t = new TabPage("Vật tư");
            txtTimVT = T(110, 12, 220);
            gridVT = G(10, 50, 920, 300);
            txtMaVT = T(100, 370, 150);
            txtTenVT = T(350, 370, 250);
            txtDVT = T(660, 370, 90);
            txtTonVT = T(100, 405, 150, true);

            gridVT.SelectionChanged += (s, e) =>
            {
                var r = gridVT.CurrentRow;
                if (r == null) return;
                txtMaVT.Text = Cell(r, "MaVatTu");
                txtTenVT.Text = Cell(r, "TenVatTu");
                txtDVT.Text = Cell(r, "DVT");
                txtTonVT.Text = Cell(r, "SoLuongTon");
            };

            t.Controls.AddRange(new Control[]
            {
                L("Tìm (mã/tên):", 10, 12), txtTimVT,
                B("Tìm", 340, 10, 70, (s, e) => SafeRun(() => LoadVatTu(txtTimVT.Text))),
                B("Hiện tất cả", 420, 10, 100, (s, e) => SafeRun(() => { txtTimVT.Clear(); LoadVatTu(""); })),
                gridVT,
                L("Mã vật tư:", 10, 370), txtMaVT, L("Tên vật tư:", 270, 370), txtTenVT,
                L("ĐVT:", 620, 370), txtDVT, L("Tồn kho:", 10, 405), txtTonVT,
                B("Thêm", 100, 455, 90, (s, e) => SafeRun(() =>
                {
                    _vtBus.Them(new VatTuDTO { MaVatTu = txtMaVT.Text, TenVatTu = txtTenVT.Text, DVT = txtDVT.Text });
                    ReloadAll();
                })),
                B("Sửa", 200, 455, 90, (s, e) => SafeRun(() =>
                {
                    _vtBus.Sua(new VatTuDTO { MaVatTu = txtMaVT.Text, TenVatTu = txtTenVT.Text, DVT = txtDVT.Text });
                    ReloadAll();
                })),
                B("Xóa", 300, 455, 90, (s, e) => SafeRun(() =>
                {
                    if (MessageBox.Show($"Xóa vật tư {txtMaVT.Text}?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                    _vtBus.Xoa(txtMaVT.Text);
                    ReloadAll();
                })),
                B("Làm mới ô nhập", 400, 455, 130, (s, e) =>
                    { txtMaVT.Clear(); txtTenVT.Clear(); txtDVT.Clear(); txtTonVT.Clear(); })
            });
            return t;
        }

        private TabPage BuildTabNCC()
        {
            var t = new TabPage("Nhà cung cấp");
            txtTimNCC = T(140, 12, 220);
            gridNCC = G(10, 50, 920, 300);
            txtMaNCC = T(100, 370, 80, true);
            txtTenNCC = T(350, 370, 300);
            txtSDTNCC = T(100, 405, 150);
            txtDiaChiNCC = T(350, 405, 300);

            gridNCC.SelectionChanged += (s, e) =>
            {
                var r = gridNCC.CurrentRow;
                if (r == null) return;
                txtMaNCC.Text = Cell(r, "MaNCC");
                txtTenNCC.Text = Cell(r, "TenNCC");
                txtSDTNCC.Text = Cell(r, "DienThoai");
                txtDiaChiNCC.Text = Cell(r, "DiaChi");
            };

            NhaCungCapDTO FromForm() => new NhaCungCapDTO
            {
                MaNCC = int.TryParse(txtMaNCC.Text, out int id) ? id : 0,
                TenNCC = txtTenNCC.Text, DienThoai = txtSDTNCC.Text, DiaChi = txtDiaChiNCC.Text
            };

            t.Controls.AddRange(new Control[]
            {
                L("Tìm (tên/điện thoại):", 10, 12), txtTimNCC,
                B("Tìm", 370, 10, 70, (s, e) => SafeRun(() => LoadNCC(txtTimNCC.Text))),
                B("Hiện tất cả", 450, 10, 100, (s, e) => SafeRun(() => { txtTimNCC.Clear(); LoadNCC(""); })),
                gridNCC,
                L("Mã NCC:", 10, 370), txtMaNCC, L("Tên NCC:", 270, 370), txtTenNCC,
                L("Điện thoại:", 10, 405), txtSDTNCC, L("Địa chỉ:", 270, 405), txtDiaChiNCC,
                B("Thêm", 100, 455, 90, (s, e) => SafeRun(() =>
                {
                    var n = FromForm(); n.MaNCC = 0;
                    _nccBus.Them(n);
                    ReloadAll();
                })),
                B("Sửa", 200, 455, 90, (s, e) => SafeRun(() => { _nccBus.Sua(FromForm()); ReloadAll(); })),
                B("Xóa", 300, 455, 90, (s, e) => SafeRun(() =>
                {
                    if (MessageBox.Show($"Xóa nhà cung cấp {txtTenNCC.Text}?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                    _nccBus.Xoa(FromForm().MaNCC);
                    ReloadAll();
                })),
                B("Làm mới ô nhập", 400, 455, 130, (s, e) =>
                    { txtMaNCC.Clear(); txtTenNCC.Clear(); txtSDTNCC.Clear(); txtDiaChiNCC.Clear(); })
            });
            return t;
        }

        private TabPage BuildTabPhieu()
        {
            var t = new TabPage("Lập phiếu kho");
            cbLoai = C(90, 12, 90);
            cbLoai.Items.AddRange(new object[] { "NHAP", "XUAT" });
            cbNCC = C(310, 12, 260);
            Bind(cbNCC, "TenNCC", "MaNCC");
            cbLoai.SelectedIndexChanged += (s, e) => cbNCC.Enabled = cbLoai.Text == "NHAP";
            cbLoai.SelectedIndex = 0;

            cbVT = C(70, 50, 360);
            Bind(cbVT, "HienThi", "MaVatTu");
            nudSL = N(465, 50, 90, 1000000000m);
            nudGia = N(630, 50, 120, 1000000000000m);

            _dtChiTiet = new DataTable();
            _dtChiTiet.Columns.Add("MaVatTu", typeof(string));
            _dtChiTiet.Columns.Add("TenVatTu", typeof(string));
            _dtChiTiet.Columns.Add("SoLuong", typeof(decimal));
            _dtChiTiet.Columns.Add("DonGia", typeof(decimal));
            _dtChiTiet.Columns.Add("ThanhTien", typeof(decimal), "SoLuong * DonGia");
            gridCT = G(10, 90, 920, 320);
            gridCT.DataSource = _dtChiTiet;
            lblTong = new Label { Left = 300, Top = 440, Width = 380, Font = new Font("Segoe UI", 11, FontStyle.Bold), Text = "Tổng tiền: 0 đ" };

            t.Controls.AddRange(new Control[]
            {
                L("Loại phiếu:", 10, 12), cbLoai, L("Nhà cung cấp:", 210, 12), cbNCC,
                L("Vật tư:", 10, 50), cbVT, L("SL:", 440, 50), nudSL, L("Đơn giá:", 565, 50), nudGia,
                B("Thêm vào phiếu", 770, 48, 150, (s, e) => SafeRun(ThemVaoPhieu)),
                gridCT,
                B("Xóa dòng chọn", 10, 435, 130, (s, e) => SafeRun(() =>
                {
                    if (gridCT.CurrentRow != null) { gridCT.Rows.Remove(gridCT.CurrentRow); CapNhatTong(); }
                })),
                lblTong,
                B("Lưu phiếu kho", 750, 432, 170, (s, e) => SafeRun(LuuPhieu))
            });
            return t;
        }

        private TabPage BuildTabCanhBao()
        {
            var t = new TabPage("Cảnh báo & báo cáo");
            nudNguong = N(230, 12, 100, 1000000000m);
            nudNguong.Value = 10;
            gridCB = G(10, 50, 920, 300);
            lblCB = new Label { Left = 10, Top = 360, AutoSize = true };
            lblTongGT = new Label { Left = 230, Top = 410, AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) };

            t.Controls.AddRange(new Control[]
            {
                L("Vật tư tồn kho dưới ngưỡng:", 10, 12), nudNguong,
                B("Lọc", 345, 10, 80, (s, e) => SafeRun(LocTonThap)),
                gridCB, lblCB,
                B("Tính tổng giá trị tồn kho", 10, 405, 210, (s, e) => SafeRun(() =>
                    lblTongGT.Text = Money(_vtBus.TinhTongGiaTriTon()))),
                lblTongGT,
                new Label { Left = 10, Top = 450, Width = 900, Height = 40,
                    Text = "Giá trị tồn = tồn kho x đơn giá nhập gần nhất của từng vật tư." }
            });
            return t;
        }

        private TabPage BuildTabLichSu()
        {
            var t = new TabPage("Lịch sử phiếu");
            gridPhieu = G(10, 10, 920, 230);
            gridLS = G(10, 270, 920, 230);
            gridPhieu.SelectionChanged += (s, e) =>
            {
                var r = gridPhieu.CurrentRow;
                if (r == null) return;
                if (int.TryParse(Cell(r, "MaPhieu"), out int ma))
                    gridLS.DataSource = _phieuBus.GetChiTiet(ma);
            };
            t.Controls.AddRange(new Control[]
            {
                gridPhieu, new Label { Left = 10, Top = 248, AutoSize = true, Text = "Chi tiết phiếu đang chọn:" }, gridLS
            });
            return t;
        }

        // ================= Nghiệp vụ =================
        private void ReloadAll()
        {
            LoadVatTu(txtTimVT.Text);
            LoadNCC(txtTimNCC.Text);
            cbNCC.DataSource = _nccBus.GetAll();
            cbVT.DataSource = _vtBus.GetForCombo();
            gridPhieu.DataSource = _phieuBus.GetDanhSachPhieu();
        }

        private void LoadVatTu(string kw) => gridVT.DataSource = _vtBus.TimKiem(kw);
        private void LoadNCC(string kw) => gridNCC.DataSource = _nccBus.TimKiem(kw);

        private void ThemVaoPhieu()
        {
            if (cbVT.SelectedItem is not DataRowView rv) throw new InvalidOperationException("Chưa có vật tư để chọn.");
            if (nudSL.Value <= 0) throw new InvalidOperationException("Số lượng phải > 0.");

            string ma = (string)rv["MaVatTu"];
            var exist = _dtChiTiet.Select($"MaVatTu = '{Esc(ma)}'");
            if (exist.Length > 0)    // vật tư đã có -> cộng dồn số lượng
            {
                exist[0]["SoLuong"] = (decimal)exist[0]["SoLuong"] + nudSL.Value;
                exist[0]["DonGia"] = nudGia.Value;
            }
            else
            {
                _dtChiTiet.Rows.Add(ma, (string)rv["TenVatTu"], nudSL.Value, nudGia.Value);
            }
            CapNhatTong();
        }

        private void CapNhatTong()
        {
            object o = _dtChiTiet.Compute("SUM(ThanhTien)", "");
            lblTong.Text = "Tổng tiền: " + Money(o == DBNull.Value ? 0 : Convert.ToDecimal(o));
        }

        private void LuuPhieu()
        {
            var ds = _dtChiTiet.Rows.Cast<DataRow>().Select(r => new ChiTietPhieuKhoDTO
            {
                MaVatTu = (string)r["MaVatTu"],
                SoLuong = (decimal)r["SoLuong"],
                DonGia = (decimal)r["DonGia"]
            }).ToList();

            int? maNCC = cbLoai.Text == "NHAP" && cbNCC.SelectedValue != null
                ? Convert.ToInt32(cbNCC.SelectedValue) : null;

            int maPhieu = _phieuBus.LuuPhieu(cbLoai.Text, maNCC, ds);   // SqlTransaction nằm trong DAL
            MessageBox.Show($"Đã lưu phiếu {cbLoai.Text} số {maPhieu}.", "Thành công");
            _dtChiTiet.Rows.Clear();
            CapNhatTong();
            ReloadAll();
        }

        private void LocTonThap()
        {
            var dt = _vtBus.GetTonThap(nudNguong.Value);
            gridCB.DataSource = dt;
            lblCB.Text = $"Có {dt.Rows.Count} vật tư dưới ngưỡng {nudNguong.Value:N2}.";
        }
    }
}
