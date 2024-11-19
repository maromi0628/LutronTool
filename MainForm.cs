using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RoomKeypadManager
{
    public partial class MainForm : Form
    {
        // structuredDataの型を明確化
        private Dictionary<string, List<DeviceData>> structuredData;
        private ComboBox roomKeySelector; // 部屋選択用
        private DataGridView deviceTable; // デバイス一覧表示用

        public MainForm()
        {
            InitializeComponent();

            if (SelectCsvFile(out string filePath))
            {
                ProcessCsvFile(filePath);
                InitializeUI();
            }
            else
            {
                MessageBox.Show("CSVファイルが選択されませんでした。アプリを終了します。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        public void InitializeComponent()
        {
            this.Text = "Room Keypad Manager";
            this.Size = new System.Drawing.Size(800, 600);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10),
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 部屋選択ラベル
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // ComboBox
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // デバイス一覧ラベル
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // DataGridViewが残りを埋める

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

        private void ProcessCsvFile(string filePath)
        {
            structuredData = new Dictionary<string, List<DeviceData>>();

            using (var reader = new StreamReader(filePath))
            {
                for (int i = 0; i < 6; i++) // 最初の6行をスキップ
                {
                    reader.ReadLine();
                }

                string line;
                string lastDeviceName = null; // 直前のデバイス名を保持

                while ((line = reader.ReadLine()) != null)
                {
                    string[] columns = line.Split(',');

                    if (columns.Length < 6)
                    {
                        continue; // 必須列がない行をスキップ
                    }

                    string fullPath = columns[0]; // A列: フルパス（部屋 + デバイス階層）
                    string model = columns[1];    // B列: モデル情報
                    string id = columns[2];       // C列: ID情報
                    string dColumn = columns[3];  // D列: 判定用列
                    string fColumn = columns[5];  // F列: ボタン情報列

                    if (string.IsNullOrWhiteSpace(fullPath) && !string.IsNullOrWhiteSpace(lastDeviceName))
                    {
                        // A列が空白の場合、直前のデバイス名を使用
                        fullPath = lastDeviceName;
                    }

                    if (!string.IsNullOrWhiteSpace(fullPath))
                    {
                        lastDeviceName = fullPath; // 現在のA列を次に使用するため保持
                    }

                    if (string.IsNullOrWhiteSpace(fullPath) || !fullPath.Contains("\\"))
                    {
                        continue; // 無効なデータをスキップ
                    }

                    string[] parts = fullPath.Split('\\');
                    string roomKey = string.Join("\\", parts.Take(parts.Length - 1)); // 部屋キー
                    string deviceName = parts.Last(); // デバイス名

                    // ボタン情報を取得
                    List<string> buttons = new List<string>();
                    if (dColumn.Contains("Button")) // D列に「Button」が含まれている場合
                    {
                        buttons.Add(fColumn); // F列の情報を追加
                    }

                    if (!structuredData.ContainsKey(roomKey))
                    {
                        structuredData[roomKey] = new List<DeviceData>();
                    }

                    // 既存デバイスを検索
                    var existingDevice = structuredData[roomKey].FirstOrDefault(d => d.DeviceName == deviceName);

                    if (existingDevice != null)
                    {
                        // 既存デバイスにボタン情報を追加
                        existingDevice.Buttons.AddRange(buttons);
                    }
                    else
                    {
                        // 新しいデバイスを追加
                        structuredData[roomKey].Add(new DeviceData
                        {
                            DeviceName = deviceName,
                            Model = model,
                            ID = id,
                            Buttons = buttons
                        });
                    }
                }
            }
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

                // テーブルにデータを追加
                deviceTable.Rows.Add(device.DeviceName, device.Model, device.ID, buttonInfo);
            }
        }

    }

    // デバイス情報を格納するクラス
    public class DeviceData
    {
        public string DeviceName { get; set; }
        public string Model { get; set; }
        public string ID { get; set; }
        public List<string> Buttons { get; set; } = new List<string>();
    }

}
