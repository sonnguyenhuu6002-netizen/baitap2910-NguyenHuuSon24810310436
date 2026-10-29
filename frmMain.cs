using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CafePOS.BUS;
using CafePOS.DTO;

namespace CafePOS.GUI
{
    public class frmMain : Form
    {
        private readonly FlowLayoutPanel flpBan = new FlowLayoutPanel();
        private readonly DataGridView dgv = new DataGridView();
        private readonly ComboBox cboDanhMuc = new ComboBox();
        private readonly ComboBox cboMon = new ComboBox();
        private readonly ComboBox cboBanDich = new ComboBox();
        private readonly NumericUpDown nudSL = new NumericUpDown();
        private readonly Label lblBan = new Label();
        private readonly Label lblTong = new Label();

        private DataSet _dsMenu;           // menu cache (mo hinh ngat ket noi)
        private DataView _dvMon;
        private int? _maBan;               // ban dang chon
        private int? _maHD;                // hoa don chua thanh toan cua ban dang chon

        public frmMain()
        {
            BuildUI();
            Load += (s, e) => { LoadMenu(); LoadBan(); };
        }

        // ================= UI =================
        private void BuildUI()
        {
            Text = "POS Cafe";
            Size = new Size(1100, 650);
            StartPosition = FormStartPosition.CenterScreen;

            flpBan.Dock = DockStyle.Left; flpBan.Width = 480;
            flpBan.AutoScroll = true; flpBan.Padding = new Padding(8);
            flpBan.BackColor = Color.WhiteSmoke;

            var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            lblBan.Dock = DockStyle.Top; lblBan.Height = 32;
            lblBan.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            lblBan.Text = "Chọn một bàn…";

            var pnlOrder = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, WrapContents = false };
            cboDanhMuc.DropDownStyle = ComboBoxStyle.DropDownList; cboDanhMuc.Width = 130;
            cboMon.DropDownStyle = ComboBoxStyle.DropDownList; cboMon.Width = 190;
            nudSL.Minimum = 1; nudSL.Maximum = 99; nudSL.Value = 1; nudSL.Width = 50;
            var btnThem = new Button { Text = "Thêm món", AutoSize = true };
            btnThem.Click += BtnThem_Click;
            pnlOrder.Controls.AddRange(new Control[] {
                cboDanhMuc, cboMon,
                new Label { Text = "SL:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) },
                nudSL, btnThem });
            cboDanhMuc.SelectedIndexChanged += (s, e) => ApplyMonFilter();

            dgv.Dock = DockStyle.Fill; dgv.ReadOnly = true; dgv.AllowUserToAddRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.RowHeadersVisible = false;

            var pnlBottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 90 };
            lblTong.Font = new Font("Segoe UI", 14, FontStyle.Bold); lblTong.ForeColor = Color.Firebrick;
            lblTong.AutoSize = true; lblTong.Text = "Tổng: 0 đ";
            lblTong.Padding = new Padding(0, 0, 20, 0);

            cboBanDich.DropDownStyle = ComboBoxStyle.DropDownList; cboBanDich.Width = 100;
            var btnChuyen = new Button { Text = "Chuyển bàn", AutoSize = true };
            var btnThanhToan = new Button
            {
                Text = "THANH TOÁN", AutoSize = true,
                BackColor = Color.SeaGreen, ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            var btnMenu = new Button { Text = "Quản lý thực đơn", AutoSize = true };
            var btnThongKe = new Button { Text = "Thống kê", AutoSize = true };
            btnThongKe.Click += (s, e) => { using (var f = new frmThongKe()) f.ShowDialog(this); };
            btnChuyen.Click += BtnChuyen_Click;
            btnThanhToan.Click += BtnThanhToan_Click;
            btnMenu.Click += (s, e) => { using (var f = new frmQuanLyMenu()) f.ShowDialog(this); LoadMenu(); };

            pnlBottom.Controls.AddRange(new Control[] { lblTong, cboBanDich, btnChuyen, btnThanhToan, btnMenu, btnThongKe });

            right.Controls.Add(dgv);
            right.Controls.Add(pnlBottom);
            right.Controls.Add(pnlOrder);
            right.Controls.Add(lblBan);
            Controls.Add(right);
            Controls.Add(flpBan);
        }

        // ================= Menu (DataSet cache) =================
        private void LoadMenu()
        {
            try
            {
                _dsMenu = MenuBUS.LoadMenuDataSet();
                _dvMon = new DataView(_dsMenu.Tables["MonAn"]);

                cboDanhMuc.DataSource = _dsMenu.Tables["DanhMuc"];
                cboDanhMuc.DisplayMember = "TenDanhMuc";
                cboDanhMuc.ValueMember = "MaDanhMuc";

                cboMon.DataSource = _dvMon;
                cboMon.DisplayMember = "TenMon";
                cboMon.ValueMember = "MaMon";
                ApplyMonFilter();
            }
            catch (Exception ex) { Error(ex); }
        }

        private void ApplyMonFilter()
        {
            if (_dvMon == null) return;
            _dvMon.RowFilter = cboDanhMuc.SelectedValue is int ma ? $"MaDanhMuc = {ma}" : "1 = 0";
        }

        // ================= So do ban =================
        private void LoadBan()
        {
            try
            {
                List<BanDTO> dsBan = BanBUS.GetAll();
                flpBan.SuspendLayout();
                flpBan.Controls.Clear();
                foreach (BanDTO ban in dsBan)
                {
                    var btn = new Button
                    {
                        Width = 100, Height = 80, Margin = new Padding(6),
                        Text = ban.TenBan + "\r\n" + ban.TrangThai,
                        BackColor = ban.Trong ? Color.LimeGreen : Color.IndianRed,
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 10, FontStyle.Bold),
                        FlatStyle = FlatStyle.Flat
                    };
                    btn.Click += (s, e) => ChonBan(ban.MaBan, ban.TenBan);
                    flpBan.Controls.Add(btn);
                }
                flpBan.ResumeLayout();

                cboBanDich.DataSource = BanBUS.GetBanTrong();
                cboBanDich.DisplayMember = "TenBan";
                cboBanDich.ValueMember = "MaBan";
            }
            catch (Exception ex) { Error(ex); }
        }

        private void ChonBan(int maBan, string tenBan)
        {
            _maBan = maBan;
            lblBan.Text = tenBan;
            LoadOrder();
        }

        // ================= Hoa don cua ban =================
        private void LoadOrder()
        {
            if (_maBan == null) return;
            try
            {
                _maHD = HoaDonBUS.GetHoaDonChuaThanhToan(_maBan.Value)?.MaHoaDon;
                DataTable dt = _maHD == null ? new DataTable() : HoaDonBUS.GetChiTiet(_maHD.Value);
                dgv.DataSource = dt;

                if (dt.Columns.Count > 0)
                {
                    dgv.Columns["MaMon"].Visible = false;
                    dgv.Columns["TenMon"].HeaderText = "Món";
                    dgv.Columns["SoLuong"].HeaderText = "SL";
                    dgv.Columns["DonGia"].HeaderText = "Đơn giá";
                    dgv.Columns["ThanhTien"].HeaderText = "Thành tiền";
                    dgv.Columns["DonGia"].DefaultCellStyle.Format = "N0";
                    dgv.Columns["ThanhTien"].DefaultCellStyle.Format = "N0";
                }
                object sum = dt.Rows.Count == 0 ? 0m : dt.Compute("SUM(ThanhTien)", "");
                lblTong.Text = $"Tổng: {Convert.ToDecimal(sum):N0} đ";
            }
            catch (Exception ex) { Error(ex); }
        }

        // ================= Su kien =================
        private void BtnThem_Click(object sender, EventArgs e)
        {
            if (_maBan == null) { Info("Vui lòng chọn bàn trước."); return; }
            if (!(cboMon.SelectedValue is int maMon)) { Info("Vui lòng chọn món."); return; }
            try
            {
                HoaDonBUS.ThemMon(_maBan.Value, maMon, (int)nudSL.Value);
                LoadOrder();
                LoadBan();                   // doi mau ban -> do
            }
            catch (Exception ex) { Error(ex); }
        }

        private void BtnThanhToan_Click(object sender, EventArgs e)
        {
            if (_maBan == null) { Info("Vui lòng chọn bàn trước."); return; }
            if (_maHD == null) { Info("Bàn này chưa có món nào."); return; }
            if (MessageBox.Show($"Thanh toán {lblBan.Text} - {lblTong.Text.Replace("Tổng: ", "")}?",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                decimal tong = HoaDonBUS.ThanhToan(_maHD, _maBan.Value);
                Info($"Thanh toán thành công: {tong:N0} đ");
                LoadOrder();
                LoadBan();                   // ban ve mau xanh
            }
            catch (Exception ex) { Error(ex); }
        }

        private void BtnChuyen_Click(object sender, EventArgs e)
        {
            if (_maBan == null) { Info("Vui lòng chọn bàn trước."); return; }
            if (!(cboBanDich.SelectedValue is int banMoi)) { Info("Không còn bàn trống."); return; }
            try
            {
                string tenMoi = ((DataRowView)cboBanDich.SelectedItem)["TenBan"].ToString();
                HoaDonBUS.ChuyenBan(_maHD, _maBan.Value, banMoi);
                _maBan = banMoi;
                lblBan.Text = tenMoi;
                LoadOrder();
                LoadBan();
            }
            catch (Exception ex) { Error(ex); }
        }

        private static void Info(string m) =>
            MessageBox.Show(m, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        private static void Error(Exception ex) =>
            MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
