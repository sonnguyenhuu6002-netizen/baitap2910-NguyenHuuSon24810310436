using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CafePOS.BUS;

namespace CafePOS.GUI
{
    /// <summary>Quan ly Danh muc + Thuc don (CRUD qua BUS -> DAL).</summary>
    public class frmQuanLyMenu : Form
    {
        private readonly DataGridView dgvDM = NewGrid(), dgvMon = NewGrid();
        private readonly TextBox txtTenDM = new TextBox();
        private readonly TextBox txtTenMon = new TextBox(), txtGia = new TextBox();
        private readonly ComboBox cboDM = new ComboBox();

        public frmQuanLyMenu()
        {
            Text = "Quản lý thực đơn";
            Size = new Size(760, 520);
            StartPosition = FormStartPosition.CenterParent;
            BuildUI();
            Load += (s, e) => Reload();
        }

        private static DataGridView NewGrid() => new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false
        };

        private static Button Btn(string text, EventHandler h)
        { var b = new Button { Text = text, AutoSize = true }; b.Click += h; return b; }

        private static FlowLayoutPanel Row(params Control[] cs)
        {
            var p = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40 };
            p.Controls.AddRange(cs);
            return p;
        }

        private static Label Lbl(string t) =>
            new Label { Text = t, AutoSize = true, Padding = new Padding(0, 6, 0, 0) };

        private void BuildUI()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };
            var tabDM = new TabPage("Danh mục");
            var tabMon = new TabPage("Món ăn");

            // --- Tab danh muc ---
            txtTenDM.Width = 220;
            tabDM.Controls.Add(dgvDM);
            tabDM.Controls.Add(Row(Lbl("Tên danh mục:"), txtTenDM,
                Btn("Thêm", (s, e) => Do(() => MenuBUS.ThemDanhMuc(txtTenDM.Text))),
                Btn("Sửa", (s, e) => Do(() => MenuBUS.SuaDanhMuc(IdOf(dgvDM, "MaDanhMuc"), txtTenDM.Text))),
                Btn("Xóa", (s, e) => Confirm(() => MenuBUS.XoaDanhMuc(IdOf(dgvDM, "MaDanhMuc"))))));
            dgvDM.SelectionChanged += (s, e) =>
            { if (dgvDM.CurrentRow != null) txtTenDM.Text = dgvDM.CurrentRow.Cells["TenDanhMuc"].Value?.ToString(); };

            // --- Tab mon an ---
            txtTenMon.Width = 170; txtGia.Width = 90;
            cboDM.DropDownStyle = ComboBoxStyle.DropDownList; cboDM.Width = 130;
            tabMon.Controls.Add(dgvMon);
            // Dock=Top: control add sau nam tren cung -> add hang nut truoc, hang nhap sau
            tabMon.Controls.Add(Row(
                Btn("Thêm", (s, e) => Do(() => MenuBUS.ThemMon(txtTenMon.Text, DmId(), txtGia.Text))),
                Btn("Sửa / Cập nhật giá", (s, e) => Do(() =>
                    MenuBUS.SuaMon(IdOf(dgvMon, "MaMon"), txtTenMon.Text, DmId(), txtGia.Text))),
                Btn("Xóa", (s, e) => Confirm(() => MenuBUS.XoaMon(IdOf(dgvMon, "MaMon"))))));
            tabMon.Controls.Add(Row(Lbl("Tên món:"), txtTenMon, Lbl("Danh mục:"), cboDM,
                Lbl("Đơn giá:"), txtGia));
            dgvMon.BringToFront();   // Fill phai nam o index cuoi de chiem phan con lai
            dgvMon.SelectionChanged += (s, e) =>
            {
                var r = dgvMon.CurrentRow; if (r == null) return;
                txtTenMon.Text = r.Cells["TenMon"].Value?.ToString();
                txtGia.Text = Convert.ToDecimal(r.Cells["DonGia"].Value).ToString("0.##");
                cboDM.SelectedValue = r.Cells["MaDanhMuc"].Value;
            };

            tabs.TabPages.Add(tabDM);
            tabs.TabPages.Add(tabMon);
            Controls.Add(tabs);
        }

        // ---------- Nap du lieu ----------
        private void Reload()
        {
            DataTable dm = MenuBUS.LoadMenuDataSet().Tables["DanhMuc"];
            dgvDM.DataSource = dm;
            dgvDM.Columns["MaDanhMuc"].HeaderText = "Mã";
            dgvDM.Columns["TenDanhMuc"].HeaderText = "Tên danh mục";

            cboDM.DataSource = dm.Copy();
            cboDM.DisplayMember = "TenDanhMuc";
            cboDM.ValueMember = "MaDanhMuc";

            dgvMon.DataSource = MenuBUS.GetMonAnView();
            dgvMon.Columns["MaMon"].HeaderText = "Mã";
            dgvMon.Columns["TenMon"].HeaderText = "Tên món";
            dgvMon.Columns["TenDanhMuc"].HeaderText = "Danh mục";
            dgvMon.Columns["DonGia"].HeaderText = "Đơn giá";
            dgvMon.Columns["DonGia"].DefaultCellStyle.Format = "N0";
            dgvMon.Columns["MaDanhMuc"].Visible = false;
        }

        // ---------- Tien ich ----------
        private static int IdOf(DataGridView g, string col)
        {
            if (g.CurrentRow == null) throw new InvalidOperationException("Vui lòng chọn một dòng.");
            return Convert.ToInt32(g.CurrentRow.Cells[col].Value);
        }

        private int DmId()
        {
            if (!(cboDM.SelectedValue is int id)) throw new ArgumentException("Vui lòng chọn danh mục.");
            return id;
        }

        private void Do(Action a)
        {
            try { a(); Reload(); }
            catch (Exception ex)
            { MessageBox.Show(ex.Message, "Không thực hiện được", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void Confirm(Action a)
        {
            if (MessageBox.Show("Bạn chắc chắn muốn xóa?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) Do(a);
        }
    }
}
