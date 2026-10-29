using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CafePOS.BUS;

namespace CafePOS.GUI
{
    /// <summary>Bao cao - Thong ke doanh thu theo ngay va top mon ban chay.</summary>
    public class frmThongKe : Form
    {
        private readonly DateTimePicker dtpTu = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110 };
        private readonly DateTimePicker dtpDen = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110 };
        private readonly DataGridView dgvNgay = NewGrid(), dgvMon = NewGrid();
        private readonly Label lblTong = new Label();

        public frmThongKe()
        {
            Text = "Báo cáo - Thống kê doanh thu";
            Size = new Size(720, 560);
            StartPosition = FormStartPosition.CenterParent;

            dtpTu.Value = DateTime.Today.AddDays(-30);
            dtpDen.Value = DateTime.Today;

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            split.Panel1.Controls.Add(dgvNgay);
            split.Panel2.Controls.Add(dgvMon);

            lblTong.Dock = DockStyle.Bottom; lblTong.Height = 36;
            lblTong.Font = new Font("Segoe UI", 12, FontStyle.Bold); lblTong.ForeColor = Color.Firebrick;
            lblTong.TextAlign = ContentAlignment.MiddleLeft;

            var btn = new Button { Text = "Xem thống kê", AutoSize = true };
            btn.Click += (s, e) => LoadData();
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42 };
            top.Controls.AddRange(new Control[] {
                new Label { Text = "Từ ngày:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, dtpTu,
                new Label { Text = "Đến ngày:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, dtpDen, btn });

            // add control Fill truoc, control Dock=Top/Bottom sau
            Controls.Add(split);
            Controls.Add(lblTong);
            Controls.Add(top);
            Load += (s, e) => { split.SplitterDistance = split.Height / 2; LoadData(); };
        }

        private static DataGridView NewGrid() => new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        private void LoadData()
        {
            try
            {
                DataTable ngay = HoaDonBUS.ThongKeDoanhThuTheoNgay(dtpTu.Value, dtpDen.Value);
                DataTable mon = HoaDonBUS.ThongKeTopMon(dtpTu.Value, dtpDen.Value);
                dgvNgay.DataSource = ngay;
                dgvMon.DataSource = mon;

                dgvNgay.Columns["Ngay"].HeaderText = "Ngày";
                dgvNgay.Columns["Ngay"].DefaultCellStyle.Format = "dd/MM/yyyy";
                dgvNgay.Columns["SoHoaDon"].HeaderText = "Số hóa đơn";
                dgvNgay.Columns["DoanhThu"].HeaderText = "Doanh thu";
                dgvNgay.Columns["DoanhThu"].DefaultCellStyle.Format = "N0";
                dgvMon.Columns["TenMon"].HeaderText = "Top món bán chạy";
                dgvMon.Columns["SoLuongBan"].HeaderText = "Số lượng bán";
                dgvMon.Columns["DoanhThu"].HeaderText = "Doanh thu";
                dgvMon.Columns["DoanhThu"].DefaultCellStyle.Format = "N0";

                object sum = ngay.Rows.Count == 0 ? 0m : ngay.Compute("SUM(DoanhThu)", "");
                lblTong.Text = $"  Tổng doanh thu: {Convert.ToDecimal(sum):N0} đ";
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }
}
