using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RoomKeypadManager
{
    public partial class MainForm : Form
    {
        // structuredData�̌^�𖾊m��
        private Dictionary<string, List<DeviceData>> structuredData;
        private ComboBox roomKeySelector; // �����I��p
        private DataGridView deviceTable; // �f�o�C�X�ꗗ�\���p

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
                MessageBox.Show("CSV�t�@�C�����I������܂���ł����B�A�v�����I�����܂��B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // �����I�����x��
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // ComboBox
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // �f�o�C�X�ꗗ���x��
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // DataGridView���c��𖄂߂�

            Label roomKeyLabel = new Label
            {
                Text = "������I�����Ă�������:",
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
                Text = "�f�o�C�X�ꗗ:",
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

            // �w�b�_�[��ݒ�
            deviceTable.Columns.Add("ColumnDeviceName", "�f�o�C�X��");
            deviceTable.Columns.Add("ColumnModel", "���f��");
            deviceTable.Columns.Add("ColumnID", "ID");
            deviceTable.Columns.Add("ColumnButtons", "�{�^�����");
            mainLayout.Controls.Add(deviceTable, 0, 3);

            this.Controls.Add(mainLayout);
        }


        private bool SelectCsvFile(out string filePath)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                Title = "CSV�t�@�C����I�����Ă�������"
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
                for (int i = 0; i < 6; i++) // �ŏ���6�s���X�L�b�v
                {
                    reader.ReadLine();
                }

                string line;
                string lastDeviceName = null; // ���O�̃f�o�C�X����ێ�

                while ((line = reader.ReadLine()) != null)
                {
                    string[] columns = line.Split(',');

                    if (columns.Length < 6)
                    {
                        continue; // �K�{�񂪂Ȃ��s���X�L�b�v
                    }

                    string fullPath = columns[0]; // A��: �t���p�X�i���� + �f�o�C�X�K�w�j
                    string model = columns[1];    // B��: ���f�����
                    string id = columns[2];       // C��: ID���
                    string dColumn = columns[3];  // D��: ����p��
                    string fColumn = columns[5];  // F��: �{�^������

                    if (string.IsNullOrWhiteSpace(fullPath) && !string.IsNullOrWhiteSpace(lastDeviceName))
                    {
                        // A�񂪋󔒂̏ꍇ�A���O�̃f�o�C�X�����g�p
                        fullPath = lastDeviceName;
                    }

                    if (!string.IsNullOrWhiteSpace(fullPath))
                    {
                        lastDeviceName = fullPath; // ���݂�A������Ɏg�p���邽�ߕێ�
                    }

                    if (string.IsNullOrWhiteSpace(fullPath) || !fullPath.Contains("\\"))
                    {
                        continue; // �����ȃf�[�^���X�L�b�v
                    }

                    string[] parts = fullPath.Split('\\');
                    string roomKey = string.Join("\\", parts.Take(parts.Length - 1)); // �����L�[
                    string deviceName = parts.Last(); // �f�o�C�X��

                    // �{�^�������擾
                    List<string> buttons = new List<string>();
                    if (dColumn.Contains("Button")) // D��ɁuButton�v���܂܂�Ă���ꍇ
                    {
                        buttons.Add(fColumn); // F��̏���ǉ�
                    }

                    if (!structuredData.ContainsKey(roomKey))
                    {
                        structuredData[roomKey] = new List<DeviceData>();
                    }

                    // �����f�o�C�X������
                    var existingDevice = structuredData[roomKey].FirstOrDefault(d => d.DeviceName == deviceName);

                    if (existingDevice != null)
                    {
                        // �����f�o�C�X�Ƀ{�^������ǉ�
                        existingDevice.Buttons.AddRange(buttons);
                    }
                    else
                    {
                        // �V�����f�o�C�X��ǉ�
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
                // �{�^�������J���}��؂�ŕ\��
                string buttonInfo = string.Join(", ", device.Buttons);

                // �e�[�u���Ƀf�[�^��ǉ�
                deviceTable.Rows.Add(device.DeviceName, device.Model, device.ID, buttonInfo);
            }
        }

    }

    // �f�o�C�X�����i�[����N���X
    public class DeviceData
    {
        public string DeviceName { get; set; }
        public string Model { get; set; }
        public string ID { get; set; }
        public List<string> Buttons { get; set; } = new List<string>();
    }

}
