using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RoomKeypadManager
{
    public partial class MainForm : Form
    {
        private Dictionary<string, List<DeviceData>> structuredData; // �����f�[�^
        private ComboBox roomKeySelector; // �����I��p
        private DataGridView deviceTable; // �f�o�C�X�ꗗ�\���p
        private FlowLayoutPanel keypadPanel; // �f�o�b�O���p�L�[�p�b�h�\���p�l��

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
                MessageBox.Show("CSV�t�@�C�����I������܂���ł����B�A�v�����I�����܂��B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // �����I�����x��
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // ComboBox
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // �f�o�C�X�ꗗ���x��
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // DataGridView���c��̔����𖄂߂�
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // �L�[�p�b�h�p�l��

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

            // �f�o�b�O���p�l��
            keypadPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            mainLayout.Controls.Add(keypadPanel, 0, 4);

            // �f�o�b�O���쐬�{�^��
            Button debugButton = new Button
            {
                Text = "�f�o�b�O���쐬",
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
                deviceTable.Rows.Add(device.DeviceName, device.Model, device.ID, buttonInfo);
            }
        }




        private void DebugButton_Click(object sender, EventArgs e)
        {
            string selectedRoomKey = roomKeySelector.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedRoomKey) || !structuredData.ContainsKey(selectedRoomKey))
            {
                MessageBox.Show("�������I������Ă��܂���B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                            Text = $"�f�o�C�X��: {device.DeviceName}",
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
                MessageBox.Show("�p���f�B�E���L�[�p�b�h�͌�����܂���ł����B", "���", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
