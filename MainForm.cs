using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RoomKeypadManager
{
    public partial class MainForm : Form
    {
        private Dictionary<string, List<DeviceData>> structuredData; // 部屋データ
        private ComboBox roomKeySelector; // 部屋選択用
        private DataGridView deviceTable; // デバイス一覧表示用
        private FlowLayoutPanel keypadPanel; // デバッグ環境用キーパッド表示パネル

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
            this.Size = new System.Drawing.Size(800, 600);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(10),
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 部屋選択ラベル
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // ComboBox
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // デバイス一覧ラベル
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // DataGridViewが残りの半分を埋める
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // キーパッドパネル

            Label roomKeyLabel = new Label
            {
                Text = "部屋を選択してください:",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 5)
            };
            mainLayout.Controls.Add(roomKeyLabel, 0, 0);

            roomKeySelector = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 5, 0, 15)
            };
            roomKeySelector.SelectedIndexChanged += RoomKeySelector_SelectedIndexChanged;
            mainLayout.Controls.Add(roomKeySelector, 0, 1);

            Label deviceListLabel = new Label
            {
                Text = "デバイス一覧:",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 5)
            };
            mainLayout.Controls.Add(deviceListLabel, 0, 2);

            deviceTable = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // ヘッダーを設定
            deviceTable.Columns.Add("ColumnDeviceName", "デバイス名");
            deviceTable.Columns.Add("ColumnModel", "モデル");
            deviceTable.Columns.Add("ColumnID", "ID");
            deviceTable.Columns.Add("ColumnButtons", "ボタン情報");
            mainLayout.Controls.Add(deviceTable, 0, 3);

            // デバッグ環境パネル
            keypadPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            mainLayout.Controls.Add(keypadPanel, 0, 4);

            // デバッグ環境作成ボタン
            Button debugButton = new Button
            {
                Text = "デバッグ環境作成",
                Dock = DockStyle.Bottom,
                Height = 40
            };
            debugButton.Click += DebugButton_Click;
            mainLayout.Controls.Add(debugButton);

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
                // ボタン情報をカンマ区切りで表示
                string buttonInfo = string.Join(", ", device.Buttons);
                deviceTable.Rows.Add(device.DeviceName, device.Model, device.ID, buttonInfo);
            }
        }




        private void DebugButton_Click(object sender, EventArgs e)
        {
            string selectedRoomKey = roomKeySelector.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedRoomKey) || !structuredData.ContainsKey(selectedRoomKey))
            {
                MessageBox.Show("部屋が選択されていません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            keypadPanel.Controls.Clear();

            var roomDevices = structuredData[selectedRoomKey];

            foreach (var device in roomDevices)
            {
                if (device.Model.StartsWith("MWP-U") || device.Model.StartsWith("MWP-B"))
                {
                    int type = 0;
                    if (device.Model.Contains("-2W")) type = 2;
                    else if (device.Model.Contains("-4W")) type = 4;

                    if (type > 0)
                    {
                        Label deviceLabel = new Label
                        {
                            Text = $"デバイス名: {device.DeviceName}",
                            AutoSize = true,
                            Margin = new Padding(10, 10, 10, 5)
                        };
                        keypadPanel.Controls.Add(deviceLabel);

                        Keypad keypad = new Keypad(type);
                        keypadPanel.Controls.Add(keypad);
                    }
                }
            }

            if (keypadPanel.Controls.Count == 0)
            {
                MessageBox.Show("パラディウムキーパッドは見つかりませんでした。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
