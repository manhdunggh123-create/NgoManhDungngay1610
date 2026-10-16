using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ngomanhdung1
{
    public class Product : INotifyPropertyChanged
    {
        private string id = "", name = "", category = "", imagePath = "";
        private int quantity;
        private decimal unitPrice;
        public string Id { get => id; set { id = value; Changed(nameof(Id)); } }
        public string Name { get => name; set { name = value; Changed(nameof(Name)); } }
        public string Category { get => category; set { category = value; Changed(nameof(Category)); } }
        public int Quantity { get => quantity; set { quantity = value; Changed(nameof(Quantity)); } }
        public decimal UnitPrice { get => unitPrice; set { unitPrice = value; Changed(nameof(UnitPrice)); } }
        public string ImagePath { get => imagePath; set { imagePath = value; Changed(nameof(ImagePath)); } }
        public decimal TotalValue => Quantity * UnitPrice;
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Changed(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    public class frmInventoryManager : Form
    {
        private readonly BindingList<Product> products = new();
        private readonly BindingSource source = new();
        private readonly ErrorProvider errors = new();
        private readonly DataGridView grid = new();
        private readonly TextBox txtId = new(), txtName = new(), txtImage = new();
        private readonly ComboBox cboCategory = new();
        private readonly NumericUpDown nudQuantity = new(), nudPrice = new();
        private readonly PictureBox picProductImage = new();
        private readonly Label lblClock = new();
        private readonly System.Windows.Forms.Timer clock = new();
        private readonly string imageFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProductImages");
        private string selectedImage = "";
        private Product? selected;
        private bool allowClose;

        public frmInventoryManager()
        {
            Text = "Quản lý kho và xuất báo cáo tổng hợp";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1200, 760);
            MinimumSize = new Size(950, 600);
            Font = new Font("Segoe UI", 9F);
            BuildUi();
            ConfigureGrid();
            source.DataSource = products;
            grid.DataSource = source;
            products.ListChanged += (_, _) => UpdateStatus();
            Directory.CreateDirectory(imageFolder);
            products.Add(new Product { Id = "SP001", Name = "Bàn phím", Category = "Điện tử", Quantity = 10, UnitPrice = 250000 });
            products.Add(new Product { Id = "SP002", Name = "Chuột máy tính", Category = "Điện tử", Quantity = 15, UnitPrice = 150000 });
            UpdateStatus();
            clock.Interval = 1000;
            clock.Tick += (_, _) => lblClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            clock.Start();
            lblClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            FormClosing += ConfirmClosing;
        }

        private void BuildUi()
        {
            var menu = new MenuStrip();
            var system = new ToolStripMenuItem("System");
            system.DropDownItems.Add("Open File", null, (_, _) => OpenJson());
            system.DropDownItems.Add("Save Report", null, (_, _) => SaveReport());
            system.DropDownItems.Add(new ToolStripSeparator());
            system.DropDownItems.Add("Exit", null, (_, _) => Close());
            var tools = new ToolStripMenuItem("Tools");
            tools.DropDownItems.Add("Import CSV", null, (_, _) => ImportCsv());
            tools.DropDownItems.Add("Export CSV", null, (_, _) => ExportCsv());
            menu.Items.Add(system); menu.Items.Add(tools);
            MainMenuStrip = menu; Controls.Add(menu);

            var tool = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            tool.Items.Add(new ToolStripButton("Open", SystemIcons.Application.ToBitmap(), (_, _) => OpenJson()));
            tool.Items.Add(new ToolStripButton("Save report", SystemIcons.Shield.ToBitmap(), (_, _) => SaveReport()));
            tool.Items.Add(new ToolStripSeparator());
            tool.Items.Add(new ToolStripButton("Import CSV", SystemIcons.Information.ToBitmap(), (_, _) => ImportCsv()));
            tool.Items.Add(new ToolStripButton("Export CSV", SystemIcons.WinLogo.ToBitmap(), (_, _) => ExportCsv()));
            tool.Items.Add(new ToolStripSeparator());
            tool.Items.Add(new ToolStripButton("Exit", SystemIcons.Error.ToBitmap(), (_, _) => Close()));
            Controls.Add(tool);

            var status = new StatusStrip();
            status.Items.Add(new ToolStripStatusLabel("Trạng thái: Sẵn sàng"));
            status.Items.Add(new ToolStripStatusLabel { Name = "inventoryStatus", Spring = true, TextAlign = ContentAlignment.MiddleLeft });
            status.Items.Add(new ToolStripStatusLabel("Thời gian:"));
            status.Items.Add(new ToolStripControlHost(lblClock));
            Controls.Add(status);

            var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(10) };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 215));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(main); main.BringToFront();

            var group = new GroupBox { Text = "Thông tin sản phẩm", Dock = DockStyle.Fill, Padding = new Padding(8) };
            main.Controls.Add(group, 0, 0);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            for (int i = 0; i < 4; i++) layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            group.Controls.Add(layout);

            txtId.PlaceholderText = "Mã sản phẩm";
            txtName.PlaceholderText = "Tên sản phẩm";
            cboCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cboCategory.Items.AddRange(new object[] { "Điện tử", "Gia dụng", "Văn phòng phẩm", "Khác" });
            cboCategory.SelectedIndex = 0;
            nudQuantity.Maximum = 1000000;
            nudPrice.Maximum = 1000000000;
            nudPrice.ThousandsSeparator = true;
            nudPrice.DecimalPlaces = 0;
            AddField(layout, "Mã sản phẩm:", txtId, 0, 0);
            AddField(layout, "Tên sản phẩm:", txtName, 2, 0);
            AddField(layout, "Danh mục:", cboCategory, 0, 1);
            AddField(layout, "Số lượng:", nudQuantity, 2, 1);
            AddField(layout, "Đơn giá (VNĐ):", nudPrice, 0, 2);

            txtImage.ReadOnly = true;
            var imageControls = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            txtImage.Width = 180;
            var choose = new Button { Text = "Chọn ảnh", AutoSize = true };
            choose.Click += (_, _) => ChooseImage();
            imageControls.Controls.Add(txtImage); imageControls.Controls.Add(choose);
            AddField(layout, "Ảnh sản phẩm:", imageControls, 2, 2);

            picProductImage.SizeMode = PictureBoxSizeMode.Zoom;
            picProductImage.BorderStyle = BorderStyle.FixedSingle;
            picProductImage.Size = new Size(85, 48);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            actions.Controls.Add(picProductImage);
            var add = new Button { Text = "Thêm", Width = 75 };
            var update = new Button { Text = "Sửa", Width = 75 };
            var delete = new Button { Text = "Xóa", Width = 75 };
            var clear = new Button { Text = "Làm mới", Width = 85 };
            add.Click += (_, _) => AddProduct();
            update.Click += (_, _) => UpdateProduct();
            delete.Click += (_, _) => DeleteProduct();
            clear.Click += (_, _) => ClearInputs();
            actions.Controls.AddRange(new Control[] { add, update, delete, clear });
            layout.Controls.Add(actions, 1, 3); layout.SetColumnSpan(actions, 3);

            var list = new GroupBox { Text = "Danh sách sản phẩm trong kho", Dock = DockStyle.Fill, Padding = new Padding(8) };
            main.Controls.Add(list, 0, 1);
            grid.Dock = DockStyle.Fill; list.Controls.Add(grid);
        }

        private static void AddField(TableLayoutPanel panel, string title, Control control, int col, int row)
        {
            panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, col, row);
            control.Dock = DockStyle.Fill;
            panel.Controls.Add(control, col + 1, row);
        }

        private void ConfigureGrid()
        {
            grid.AutoGenerateColumns = false;
            grid.AllowUserToAddRows = false;
            grid.ReadOnly = true;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowHeadersVisible = false;
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.Id), HeaderText = "Mã SP", FillWeight = 65 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.Name), HeaderText = "Tên sản phẩm", FillWeight = 130 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.Category), HeaderText = "Danh mục", FillWeight = 95 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.Quantity), HeaderText = "Số lượng", FillWeight = 65 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.UnitPrice), HeaderText = "Đơn giá (VNĐ)", FillWeight = 95, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.TotalValue), HeaderText = "Thành tiền (VNĐ)", FillWeight = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Product.ImagePath), HeaderText = "Tên file ảnh", FillWeight = 110 });
            grid.SelectionChanged += (_, _) =>
            {
                if (grid.CurrentRow?.DataBoundItem is not Product p) return;
                selected = p; txtId.Text = p.Id; txtName.Text = p.Name;
                if (cboCategory.Items.Contains(p.Category)) cboCategory.SelectedItem = p.Category;
                nudQuantity.Value = Math.Clamp(p.Quantity, 0, (int)nudQuantity.Maximum);
                nudPrice.Value = Math.Clamp(p.UnitPrice, nudPrice.Minimum, nudPrice.Maximum);
                selectedImage = p.ImagePath;
                txtImage.Text = p.ImagePath;
                LoadImage(p.ImagePath);
            };
        }

        private void ChooseImage()
        {
            using var dlg = new OpenFileDialog { Title = "Chọn ảnh sản phẩm", Filter = "Ảnh (*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                string dest = Path.Combine(imageFolder, Guid.NewGuid().ToString("N") + Path.GetExtension(dlg.FileName));
                File.Copy(dlg.FileName, dest, true);
                selectedImage = Path.GetFileName(dest);
                txtImage.Text = selectedImage;
                LoadImage(selectedImage);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Lỗi ảnh", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void LoadImage(string? path)
        {
            var old = picProductImage.Image; picProductImage.Image = null; old?.Dispose();
            if (string.IsNullOrWhiteSpace(path)) return;
            string actual = Path.IsPathRooted(path) ? path : Path.Combine(imageFolder, path);
            if (!File.Exists(actual)) return;
            try { using var stream = File.OpenRead(actual); using var img = Image.FromStream(stream); picProductImage.Image = new Bitmap(img); } catch { }
        }

        private bool ValidateInput(bool unique)
        {
            errors.Clear(); bool ok = true;
            if (string.IsNullOrWhiteSpace(txtId.Text)) { errors.SetError(txtId, "Nhập mã sản phẩm."); ok = false; }
            else if (unique && products.Any(p => p.Id.Equals(txtId.Text.Trim(), StringComparison.OrdinalIgnoreCase))) { errors.SetError(txtId, "Mã sản phẩm đã tồn tại."); ok = false; }
            if (string.IsNullOrWhiteSpace(txtName.Text)) { errors.SetError(txtName, "Nhập tên sản phẩm."); ok = false; }
            if (cboCategory.SelectedIndex < 0) { errors.SetError(cboCategory, "Chọn danh mục."); ok = false; }
            return ok;
        }

        private Product ReadForm() => new()
        {
            Id = txtId.Text.Trim(), Name = txtName.Text.Trim(), Category = cboCategory.Text,
            Quantity = (int)nudQuantity.Value, UnitPrice = nudPrice.Value, ImagePath = selectedImage
        };

        private void AddProduct()
        {
            if (!ValidateInput(true)) return;
            products.Add(ReadForm());
            MessageBox.Show(this, "Đã thêm sản phẩm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInputs();
        }

        private void UpdateProduct()
        {
            if (selected == null || !products.Contains(selected)) { MessageBox.Show(this, "Hãy chọn sản phẩm trong bảng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!ValidateInput(false)) return;
            if (products.Any(p => !ReferenceEquals(p, selected) && p.Id.Equals(txtId.Text.Trim(), StringComparison.OrdinalIgnoreCase))) { errors.SetError(txtId, "Mã sản phẩm đã tồn tại."); return; }
            var form = ReadForm();
            selected.Id = form.Id; selected.Name = form.Name; selected.Category = form.Category;
            selected.Quantity = form.Quantity; selected.UnitPrice = form.UnitPrice; selected.ImagePath = form.ImagePath;
            source.ResetBindings(false); UpdateStatus();
            MessageBox.Show(this, "Đã cập nhật sản phẩm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteProduct()
        {
            if (selected == null || !products.Contains(selected)) { MessageBox.Show(this, "Hãy chọn sản phẩm cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show(this, $"Bạn có chắc muốn xóa {selected.Name}?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            { products.Remove(selected); ClearInputs(); }
        }

        private void ClearInputs()
        {
            errors.Clear(); selected = null; txtId.Clear(); txtName.Clear();
            if (cboCategory.Items.Count > 0) cboCategory.SelectedIndex = 0;
            nudQuantity.Value = 0; nudPrice.Value = 0; selectedImage = ""; txtImage.Clear();
            LoadImage(null); grid.ClearSelection();
        }

        private void UpdateStatus()
        {
            if (Controls.OfType<StatusStrip>().FirstOrDefault()?.Items["inventoryStatus"] is ToolStripStatusLabel label)
                label.Text = $"Mã hàng: {products.Count} | Tổng số lượng: {products.Sum(p => p.Quantity)} | Tổng giá trị kho: {products.Sum(p => p.TotalValue):N0} đ";
        }

        private static string CsvEscape(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";

        private void ExportCsv()
        {
            using var dlg = new SaveFileDialog { Title = "Xuất kho CSV", Filter = "CSV (*.csv)|*.csv", FileName = "BaoCaoKho.csv", DefaultExt = "csv", AddExtension = true };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try { WriteCsv(dlg.FileName); MessageBox.Show(this, "Xuất CSV thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Lỗi xuất CSV", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void WriteCsv(string path)
        {
            using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
            writer.WriteLine("Id,Name,Category,Quantity,UnitPrice,ImagePath");
            foreach (var p in products)
                writer.WriteLine($"{CsvEscape(p.Id)},{CsvEscape(p.Name)},{CsvEscape(p.Category)},{p.Quantity.ToString(CultureInfo.InvariantCulture)},{p.UnitPrice.ToString(CultureInfo.InvariantCulture)},{CsvEscape(p.ImagePath)}");
        }

        private void ImportCsv()
        {
            using var dlg = new OpenFileDialog { Title = "Nhập kho từ CSV", Filter = "CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var lines = File.ReadAllLines(dlg.FileName, Encoding.UTF8);
                if (lines.Length == 0) throw new InvalidDataException("File CSV trống.");
                var imported = new List<Product>();
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    var f = ParseCsv(lines[i]);
                    if (f.Count < 6) throw new InvalidDataException($"Dòng {i + 1} không đủ 6 cột.");
                    if (!int.TryParse(f[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty) || qty < 0) throw new InvalidDataException($"Số lượng dòng {i + 1} không hợp lệ.");
                    if (!decimal.TryParse(f[4], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price) || price < 0) throw new InvalidDataException($"Đơn giá dòng {i + 1} không hợp lệ.");
                    if (string.IsNullOrWhiteSpace(f[0]) || string.IsNullOrWhiteSpace(f[1])) throw new InvalidDataException($"Thiếu mã/tên ở dòng {i + 1}.");
                    if (imported.Any(p => p.Id.Equals(f[0], StringComparison.OrdinalIgnoreCase))) throw new InvalidDataException($"Mã sản phẩm trùng: {f[0]}.");
                    imported.Add(new Product { Id = f[0], Name = f[1], Category = f[2], Quantity = qty, UnitPrice = price, ImagePath = f[5] });
                }
                products.Clear(); foreach (var p in imported) products.Add(p);
                MessageBox.Show(this, $"Nhập thành công {imported.Count} sản phẩm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, "Không thể nhập CSV: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private static List<string> ParseCsv(string line)
        {
            var result = new List<string>(); var current = new StringBuilder(); bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else quoted = !quoted;
                }
                else if (c == ',' && !quoted) { result.Add(current.ToString()); current.Clear(); }
                else current.Append(c);
            }
            result.Add(current.ToString()); return result;
        }

        private void SaveReport()
        {
            using var dlg = new SaveFileDialog { Title = "Lưu báo cáo tổng hợp", Filter = "JSON (*.json)|*.json|CSV (*.csv)|*.csv|Text (*.txt)|*.txt", FileName = "BaoCaoTongHop.json", DefaultExt = "json", AddExtension = true };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                string ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
                if (ext == ".csv") WriteCsv(dlg.FileName);
                else if (ext == ".txt")
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("BÁO CÁO TỔNG HỢP KHO");
                    sb.AppendLine("Thời gian: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                    sb.AppendLine("Số mã hàng: " + products.Count);
                    sb.AppendLine("Tổng số lượng: " + products.Sum(p => p.Quantity));
                    sb.AppendLine("Tổng giá trị kho: " + products.Sum(p => p.TotalValue).ToString("N0") + " VNĐ");
                    foreach (var p in products) sb.AppendLine($"{p.Id} | {p.Name} | {p.Category} | SL: {p.Quantity} | Đơn giá: {p.UnitPrice:N0} | Ảnh: {p.ImagePath}");
                    File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
                }
                else
                {
                    var report = new { CreatedAt = DateTime.Now, ProductCodeCount = products.Count, TotalQuantity = products.Sum(p => p.Quantity), TotalInventoryValue = products.Sum(p => p.TotalValue), Products = products.ToList() };
                    File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(true));
                }
                MessageBox.Show(this, "Lưu báo cáo thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, "Không thể lưu báo cáo: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void OpenJson()
        {
            using var dlg = new OpenFileDialog { Title = "Mở dữ liệu kho JSON", Filter = "JSON (*.json)|*.json|Tất cả tệp (*.*)|*.*" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(dlg.FileName, Encoding.UTF8));
                if (!doc.RootElement.TryGetProperty("Products", out var arr) || arr.ValueKind != JsonValueKind.Array) throw new InvalidDataException("JSON không có danh sách Products.");
                var imported = new List<Product>();
                foreach (var item in arr.EnumerateArray())
                    imported.Add(new Product
                    {
                        Id = item.TryGetProperty("Id", out var id) ? id.GetString() ?? "" : "",
                        Name = item.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : "",
                        Category = item.TryGetProperty("Category", out var cat) ? cat.GetString() ?? "Khác" : "Khác",
                        Quantity = item.TryGetProperty("Quantity", out var qty) ? qty.GetInt32() : 0,
                        UnitPrice = item.TryGetProperty("UnitPrice", out var price) ? price.GetDecimal() : 0,
                        ImagePath = item.TryGetProperty("ImagePath", out var img) ? img.GetString() ?? "" : ""
                    });
                products.Clear(); foreach (var p in imported) products.Add(p);
                MessageBox.Show(this, $"Đã mở {imported.Count} sản phẩm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, "Không thể mở file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ConfirmClosing(object? sender, FormClosingEventArgs e)
        {
            if (allowClose) return;
            if (MessageBox.Show(this, "Bạn có chắc chắn muốn thoát chương trình?", "Xác nhận thoát", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            { allowClose = true; clock.Stop(); picProductImage.Image?.Dispose(); }
            else e.Cancel = true;
        }
    }
}
