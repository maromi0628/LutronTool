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
                MessageBox.Show("CSV�t�@�C�����I������܂���ł����B�A�v�����I�����܂��B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // �����I�����x��
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // ComboBox
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // �f�o�C�X�ꗗ
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // �f�o�b�O���쐬�{�^��
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // �f�o�b�O�p�l��

            // �����I�����x��
            Label roomKeyLabel = new Label
            {
                Text = "������I�����Ă�������:",
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 5)
            };
            mainLayout.Controls.Add(roomKeyLabel, 0, 0);

            // �����I��ComboBox
            roomKeySelector = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 5, 0, 15)
            };
            roomKeySelector.SelectedIndexChanged += RoomKeySelector_SelectedIndexChanged;
            mainLayout.Controls.Add(roomKeySelector, 0, 1);

            // �f�o�C�X�ꗗ(DataGridView)
            deviceTable = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            deviceTable.Columns.Add("ColumnDeviceName", "�f�o�C�X��");
            deviceTable.Columns.Add("ColumnModel", "���f��");
            deviceTable.Columns.Add("ColumnID", "ID");
            deviceTable.Columns.Add("ColumnButtons", "�{�^�����");
            mainLayout.Controls.Add(deviceTable, 0, 2);

            // �f�o�b�O���쐬�{�^��
            Button createDebugEnvironmentButton = new Button
            {
                Text = "�f�o�b�O���쐬",
                Dock = DockStyle.Top,
                Height = 40
            };
            createDebugEnvironmentButton.Click += CreateDebugEnvironmentButton_Click;
            mainLayout.Controls.Add(createDebugEnvironmentButton, 0, 3);

            // �f�o�b�O�p�l��
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
                string buttonInfo = string.Join(", ", device.Buttons);
                deviceTable.Rows.Add(device.DeviceName, device.Model, device.ID, buttonInfo);
            }
        }

        private void CreateDebugEnvironmentButton_Click(object sender, EventArgs e)
        {
            if (roomKeySelector.SelectedItem == null)
            {
                MessageBox.Show("������I�����Ă��������B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string selectedRoomKey = roomKeySelector.SelectedItem.ToString();
            if (!structuredData.ContainsKey(selectedRoomKey))
            {
                MessageBox.Show("�I�����������̃f�o�C�X��������܂���B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // �f�o�b�O�p�l�����N���A
            debugPanel.Controls.Clear();

            // FlowLayoutPanel�ŉ����тɃL�[�p�b�h��\��
            FlowLayoutPanel keypadFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoScroll = true,
                WrapContents = false
            };

            // �f�o�C�X�����ƂɃO���[�v��
            var deviceGroups = structuredData[selectedRoomKey]
                .GroupBy(device => device.DeviceName.Split('(')[0]) // �f�o�C�X������ɃO���[�v��
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var group in deviceGroups)
            {
                // �O���[�v�{�b�N�X�i�g���t���j
                GroupBox groupBox = new GroupBox
                {
                    Text = group.Key, // �f�o�C�X���iSW1, SW2�Ȃǁj
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Margin = new Padding(10),
                    Padding = new Padding(10)
                };

                // �O���[�v���̃L�[�p�b�h�������тɔz�u
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
                        // �L�[�p�b�h���쐬
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
            debugPanel.Update(); // ���C�A�E�g�������X�V
        }


    }
}
