using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace ngomanhdung1
{
    public class Device : INotifyPropertyChanged
    {
        private string deviceId = "";
        private string deviceName = "";
        private string category = "";
        private decimal price;
        private int quantity;

        public string DeviceId
        {
            get => deviceId;
            set { deviceId = value; OnPropertyChanged(); }
        }

        public string DeviceName
        {
            get => deviceName;
            set { deviceName = value; OnPropertyChanged(); }
        }

        public string Category
        {
            get => category;
            set { category = value; OnPropertyChanged(); }
        }

        public decimal Price
        {
            get => price;
            set { price = value; OnPropertyChanged(); }
        }

        public int Quantity
        {
            get => quantity;
            set { quantity = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [System.Runtime.CompilerServices.CallerMemberName]
            string? name = null)
        {
            PropertyChanged?.Invoke(
                this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class Form1 : Form
    {
        private readonly BindingList<Device> devices = new();
        private readonly BindingSource source = new();
        private readonly ErrorProvider errors = new();

        private readonly TextBox txtDeviceId = new();
        private readonly TextBox txtDeviceName = new();
        private readonly ComboBox cboCategory = new();
        private readonly TextBox txtPrice = new();
        private readonly NumericUpDown numQuantity = new();

        private readonly DataGridView dgvDevices = new();

        private readonly Button btnAdd = new();
        private readonly Button btnUpdate = new();
        private readonly Button btnDelete = new();
        private readonly Button btnClear = new();

        private Device? selectedDevice;
        private int nextId = 1;
        private bool loading;

        public Form1()
        {
            InitializeComponent();
            Controls.Clear();

            BuildInterface();
            ConfigureGrid();

            source.DataSource = devices;
            dgvDevices.DataSource = source;

            cboCategory.Items.AddRange(new object[]
            {
                "Laptop",
                "Điện thoại",
                "Máy tính bảng",
                "Màn hình",
                "Phụ kiện"
            });

            errors.ContainerControl = this;
            errors.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            dgvDevices.SelectionChanged += GridSelectionChanged;
            btnAdd.Click += AddDevice;
            btnUpdate.Click += UpdateDevice;
            btnDelete.Click += DeleteDevice;
            btnClear.Click += ClearForm;

            txtDeviceName.Validating += ValidateName;
            cboCategory.Validating += ValidateCategory;
            txtPrice.Validating += ValidatePrice;

            devices.Add(new Device
            {
                DeviceId = "TB001",
                DeviceName = "Laptop Dell",
                Category = "Laptop",
                Price = 15000000,
                Quantity = 5
            });

            devices.Add(new Device
            {
                DeviceId = "TB002",
                DeviceName = "iPhone",
                Category = "Điện thoại",
                Price = 10000000,
                Quantity = 3
            });

            nextId = 3;
            ClearInputs();
        }

        private void BuildInterface()
        {
            Text = "QUẢN LÝ DANH MỤC THIẾT BỊ ĐIỆN TỬ";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1100, 720);
            MinimumSize = new Size(900, 600);
            Font = new Font("Segoe UI", 10);

            TableLayoutPanel main = new TableLayoutPanel
            {
                Name = "mainLayout",
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(8),
                Margin = new Padding(0)
            };

            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 185));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Controls.Add(main);

            Label title = new Label
            {
                Text = "QUẢN LÝ THIẾT BỊ ĐIỆN TỬ",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 18, FontStyle.Bold)
            };
            main.Controls.Add(title, 0, 0);

            GroupBox group = new GroupBox
            {
                Text = "Thông tin thiết bị",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            main.Controls.Add(group, 0, 1);

            TableLayoutPanel inputs = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 3,
                Padding = new Padding(5)
            };

            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            for (int i = 0; i < 3; i++)
                inputs.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

            group.Controls.Add(inputs);

            txtDeviceId.ReadOnly = true;
            cboCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            numQuantity.Minimum = 0;
            numQuantity.Maximum = 1000000;

            AddField(inputs, "Mã thiết bị:", txtDeviceId, 0, 0);
            AddField(inputs, "Loại thiết bị:", cboCategory, 2, 0);
            AddField(inputs, "Tên thiết bị:", txtDeviceName, 0, 1);
            AddField(inputs, "Đơn giá:", txtPrice, 2, 1);
            AddField(inputs, "Số lượng tồn:", numQuantity, 0, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(3)
            };

            btnAdd.Text = "Thêm mới";
            btnUpdate.Text = "Cập nhật";
            btnDelete.Text = "Xóa";
            btnClear.Text = "Làm mới / Hủy";

            foreach (Button button in new[] { btnAdd, btnUpdate, btnDelete, btnClear })
            {
                button.Size = new Size(150, 36);
                buttons.Controls.Add(button);
            }

            main.Controls.Add(buttons, 0, 2);

            dgvDevices.Dock = DockStyle.Fill;
            dgvDevices.Visible = true;
            dgvDevices.Margin = new Padding(3);
            main.Controls.Add(dgvDevices, 0, 3);
        }

        private void AddField(TableLayoutPanel panel, string caption, Control input, int labelColumn, int row)
        {
            Label label = new Label
            {
                Text = caption,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(3, 5, 8, 5);

            panel.Controls.Add(label, labelColumn, row);
            panel.Controls.Add(input, labelColumn + 1, row);
        }

        private void ConfigureGrid()
        {
            dgvDevices.AutoGenerateColumns = false;
            dgvDevices.Columns.Clear();
            dgvDevices.ReadOnly = true;
            dgvDevices.AllowUserToAddRows = false;
            dgvDevices.AllowUserToDeleteRows = false;
            dgvDevices.MultiSelect = false;
            dgvDevices.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDevices.RowHeadersVisible = false;
            dgvDevices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDevices.BackgroundColor = Color.White;
            dgvDevices.BorderStyle = BorderStyle.FixedSingle;
            dgvDevices.Visible = true;
            dgvDevices.ColumnHeadersVisible = true;
            dgvDevices.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDeviceId",
                HeaderText = "Mã thiết bị",
                DataPropertyName = "DeviceId",
                FillWeight = 75
            });

            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDeviceName",
                HeaderText = "Tên thiết bị",
                DataPropertyName = "DeviceName",
                FillWeight = 150
            });

            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCategory",
                HeaderText = "Loại thiết bị",
                DataPropertyName = "Category",
                FillWeight = 100
            });

            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPrice",
                HeaderText = "Đơn giá",
                DataPropertyName = "Price",
                FillWeight = 100,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N0",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colQuantity",
                HeaderText = "Số lượng tồn",
                DataPropertyName = "Quantity",
                FillWeight = 85
            });
        }

        private bool ValidateInputs()
        {
            errors.Clear();
            bool valid = true;

            if (string.IsNullOrWhiteSpace(txtDeviceName.Text))
            {
                errors.SetError(txtDeviceName, "Tên thiết bị không được để trống.");
                valid = false;
            }

            if (cboCategory.SelectedIndex < 0)
            {
                errors.SetError(cboCategory, "Vui lòng chọn loại thiết bị.");
                valid = false;
            }

            if (!decimal.TryParse(txtPrice.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal price) || price < 0)
            {
                errors.SetError(txtPrice, "Đơn giá phải là số hợp lệ và không âm.");
                valid = false;
            }

            return valid;
        }

        private void GridSelectionChanged(object? sender, EventArgs e)
        {
            if (loading)
                return;

            if (dgvDevices.CurrentRow?.DataBoundItem is Device device)
            {
                selectedDevice = device;
                LoadInputs(device);
            }
        }

        private void LoadInputs(Device device)
        {
            loading = true;
            txtDeviceId.Text = device.DeviceId;
            txtDeviceName.Text = device.DeviceName;
            cboCategory.SelectedItem = device.Category;
            txtPrice.Text = device.Price.ToString("0.##", CultureInfo.CurrentCulture);
            numQuantity.Value = device.Quantity;
            errors.Clear();
            loading = false;
        }

        private void AddDevice(object? sender, EventArgs e)
        {
            if (!ValidateInputs())
                return;

            decimal.TryParse(txtPrice.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal price);

            Device device = new Device
            {
                DeviceId = "TB" + nextId.ToString("D3"),
                DeviceName = txtDeviceName.Text.Trim(),
                Category = cboCategory.SelectedItem!.ToString()!,
                Price = price,
                Quantity = (int)numQuantity.Value
            };

            nextId++;
            devices.Add(device);
            source.Position = devices.IndexOf(device);
            selectedDevice = device;
            LoadInputs(device);

            MessageBox.Show("Thêm thiết bị thành công!", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateDevice(object? sender, EventArgs e)
        {
            if (selectedDevice == null)
            {
                MessageBox.Show("Vui lòng chọn thiết bị cần cập nhật.");
                return;
            }

            if (!ValidateInputs())
                return;

            decimal.TryParse(txtPrice.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal price);

            selectedDevice.DeviceName = txtDeviceName.Text.Trim();
            selectedDevice.Category = cboCategory.SelectedItem!.ToString()!;
            selectedDevice.Price = price;
            selectedDevice.Quantity = (int)numQuantity.Value;

            source.ResetCurrentItem();

            MessageBox.Show("Cập nhật thiết bị thành công!", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteDevice(object? sender, EventArgs e)
        {
            if (selectedDevice == null)
            {
                MessageBox.Show("Vui lòng chọn thiết bị cần xóa.");
                return;
            }

            DialogResult result = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa thiết bị " + selectedDevice.DeviceName + " không?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                devices.Remove(selectedDevice);
                ClearInputs();
            }
        }

        private void ClearForm(object? sender, EventArgs e)
        {
            ClearInputs();
        }

        private void ClearInputs()
        {
            loading = true;
            selectedDevice = null;
            txtDeviceId.Clear();
            txtDeviceName.Clear();
            cboCategory.SelectedIndex = -1;
            txtPrice.Clear();
            numQuantity.Value = 0;
            errors.Clear();
            dgvDevices.ClearSelection();
            dgvDevices.CurrentCell = null;
            loading = false;
            txtDeviceName.Focus();
        }

        private void ValidateName(object? sender, CancelEventArgs e)
        {
            errors.SetError(txtDeviceName,
                string.IsNullOrWhiteSpace(txtDeviceName.Text)
                    ? "Không được để trống tên thiết bị."
                    : "");
        }

        private void ValidateCategory(object? sender, CancelEventArgs e)
        {
            errors.SetError(cboCategory,
                cboCategory.SelectedIndex < 0
                    ? "Vui lòng chọn loại thiết bị."
                    : "");
        }

        private void ValidatePrice(object? sender, CancelEventArgs e)
        {
            bool valid = decimal.TryParse(txtPrice.Text, NumberStyles.Number,
                CultureInfo.CurrentCulture, out decimal price) && price >= 0;

            errors.SetError(txtPrice, valid ? "" : "Đơn giá không hợp lệ.");
        }
    }
}
