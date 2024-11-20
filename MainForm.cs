using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RoomKeypadManager
{
    public partial class MainForm : Form
    {
        private Dictionary<string, List<DeviceData>> structuredData;
        private ComboBox roomKeySelector;
        private DataGridView deviceTable;
        private Panel debugPanel;

        public MainForm()
        {
            InitializeComponent();

            if (SelectCsvFile(out string filePath))
            {
                structuredData = CsvProcessor.ProcessCsvFile(filePath);
                InitializeUI();
            }
            else
            {
                MessageBox.Show("CSVファイルが選択されませんでした。アプリを終了します。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Room Keypad Manager";
            this.Size = new System.Drawing.Size(1000, 800);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(10),
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 部屋選択ラベル
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // ComboBox
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // デバイス一覧
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // デバッグ環境作成ボタン
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // デバッグパネル

            // 部屋選択ラベル
            Label roomKeyLabel = new Label
            {
                Text = "部屋を選択してください:",
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 5)
            };
            mainLayout.Controls.Add(roomKeyLabel, 0, 0);

            // 部屋選択ComboBox
            roomKeySelector = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 5, 0, 15)
            };
            roomKeySelector.SelectedIndexChanged += RoomKeySelector_SelectedIndexChanged;
            mainLayout.Controls.Add(roomKeySelector, 0, 1);

            // デバイス一覧(DataGridView)
            deviceTable = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            deviceTable.Columns.Add("ColumnDeviceName", "デバイス名");
            deviceTable.Columns.Add("ColumnModel", "モデル");
            deviceTable.Columns.Add("ColumnID", "ID");
            deviceTable.Columns.Add("ColumnButtons", "ボタン情報");
            mainLayout.Controls.Add(deviceTable, 0, 2);

            // デバッグ環境作成ボタン
            Button createDebugEnvironmentButton = new Button
            {
                Text = "デバッグ環境作成",
                Dock = DockStyle.Top,
                Height = 40
            };
            createDebugEnvironmentButton.Click += CreateDebugEnvironmentButton_Click;
            mainLayout.Controls.Add(createDebugEnvironmentButton, 0, 3);

            // デバッグパネル
            debugPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            mainLayout.Controls.Add(debugPanel, 0, 4);

            this.Controls.Add(mainLayout);
        }

        private bool SelectCsvFile(out string filePath)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                Title = "CSVファイルを選択してください"
            })
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;
                    return true;
                }
            }
            filePath = null;
            return false;
        }

        private void InitializeUI()
        {
            roomKeySelector.Items.AddRange(structuredData.Keys.ToArray());

            if (roomKeySelector.Items.Count > 0)
            {
                roomKeySelector.SelectedIndex = 0;
            }
        }

        private void RoomKeySelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedRoomKey = roomKeySelector.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedRoomKey) && structuredData.ContainsKey(selectedRoomKey))
            {
                DisplayDevices(selectedRoomKey);
            }
        }

        private void DisplayDevices(string roomKey)
        {
            deviceTable.Rows.Clear();

            foreach (var device in structuredData[roomKey])
            {
                string buttonInfo = string.Join(", ", device.Buttons);
                deviceTable.Rows.Add(device.DeviceName, device.Model, device.ID, buttonInfo);
            }
        }

        private void CreateDebugEnvironmentButton_Click(object sender, EventArgs e)
        {
            if (roomKeySelector.SelectedItem == null)
            {
                MessageBox.Show("部屋を選択してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string selectedRoomKey = roomKeySelector.SelectedItem.ToString();
            if (!structuredData.ContainsKey(selectedRoomKey))
            {
                MessageBox.Show("選択した部屋のデバイスが見つかりません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // デバッグパネルをクリア
            debugPanel.Controls.Clear();

            // FlowLayoutPanelで横並びにキーパッドを表示
            FlowLayoutPanel keypadFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoScroll = true,
                WrapContents = false
            };

            // デバイス名ごとにグループ化
            var deviceGroups = structuredData[selectedRoomKey]
                .GroupBy(device => device.DeviceName.Split('(')[0]) // デバイス名を基準にグループ化
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var group in deviceGroups)
            {
                // グループボックス（枠線付き）
                GroupBox groupBox = new GroupBox
                {
                    Text = group.Key, // デバイス名（SW1, SW2など）
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Margin = new Padding(10),
                    Padding = new Padding(10)
                };

                // グループ内のキーパッドを横並びに配置
                FlowLayoutPanel groupFlowPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.LeftToRight,
                    AutoSize = true,
                    WrapContents = true
                };

                foreach (var device in group.Value)
                {
                    if (device.Model.StartsWith("MWP-U") || device.Model.StartsWith("MWP-B"))
                    {
                        // キーパッドを作成
                        int buttonCount = device.Model.Contains("4W") ? 4 :
                                          device.Model.Contains("3W") ? 3 : 2;

                        Keypad keypad = new Keypad(buttonCount, device.Buttons)
                        {
                            Margin = new Padding(5)
                        };

                        groupFlowPanel.Controls.Add(keypad);
                    }
                }

                groupBox.Controls.Add(groupFlowPanel);
                keypadFlowPanel.Controls.Add(groupBox);
            }

            debugPanel.Controls.Add(keypadFlowPanel);
            debugPanel.Update(); // レイアウトを強制更新
        }


    }
}
