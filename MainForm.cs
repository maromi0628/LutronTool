using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Windows.Forms;

namespace RoomKeypadManager
{
    public partial class MainForm : Form
    {
        private Dictionary<string, List<DeviceData>> structuredData;
        private ComboBox roomKeySelector;
        private DataGridView deviceTable;
        private Panel debugPanel;
        private ListBox logListBox;
        private TelnetClientHelper telnetClientHelper = null;
        private bool isConnected = false; // �ڑ���Ԃ��Ǘ�

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
            this.WindowState = FormWindowState.Maximized;

            // SplitContainer�ŉ�ʂ����E�ɕ���
            SplitContainer mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = (int)(this.Width * 0.7), // ����70%���w��
                FixedPanel = FixedPanel.None
            };

            // �����G���A: 4����
            TableLayoutPanel leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10)
            };
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40)); // CSV���
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 5));  // �f�o�b�O�쐬�{�^��
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 5));  // Telnet�ڑ��p
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // �L�[�p�b�h�\��

            // CSV���G���A
            TableLayoutPanel csvInfoLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            csvInfoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // �����I�����x��
            csvInfoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // ComboBox
            csvInfoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // DataGridView

            Label roomKeyLabel = new Label
            {
                Text = "������I�����Ă�������:",
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 5)
            };
            csvInfoLayout.Controls.Add(roomKeyLabel, 0, 0);

            roomKeySelector = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 5, 0, 15)
            };
            roomKeySelector.SelectedIndexChanged += RoomKeySelector_SelectedIndexChanged;
            csvInfoLayout.Controls.Add(roomKeySelector, 0, 1);

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
            csvInfoLayout.Controls.Add(deviceTable, 0, 2);

            leftLayout.Controls.Add(csvInfoLayout, 0, 0);

            // �f�o�b�O���쐬�{�^��
            Button createDebugEnvironmentButton = new Button
            {
                Text = "�f�o�b�O���쐬",
                Dock = DockStyle.Fill,
                Height = 40
            };
            createDebugEnvironmentButton.Click += CreateDebugEnvironmentButton_Click;
            leftLayout.Controls.Add(createDebugEnvironmentButton, 0, 1);

            // Telnet�ڑ��p�G���A
            // Telnet�ڑ��p�G���A
            TableLayoutPanel telnetLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6, // �{�^����������̂�6��ɐݒ�
                RowCount = 1,
                Padding = new Padding(10)
            };
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // IP���̓t�B�[���h
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // �ڑ��{�^��
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // GETTIME�{�^��
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // SET DAY�{�^��
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // SET NIGHT�{�^��
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));   // �X�e�[�^�X���x��

            // IP���̓t�B�[���h
            TextBox ipAddressTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "IP�A�h���X����͂��Ă�������",
                Margin = new Padding(5)
            };
            telnetLayout.Controls.Add(ipAddressTextBox, 0, 0);

            // �ڑ��{�^��
            Button connectTelnetButton = new Button
            {
                Text = "Telnet�ڑ�",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Height = 30
            };
            telnetLayout.Controls.Add(connectTelnetButton, 1, 0);

            // GETTIME�{�^��
            Button getTimeButton = new Button
            {
                Text = "GETTIME",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Height = 30
            };
            telnetLayout.Controls.Add(getTimeButton, 2, 0);

            // SET DAY�{�^��
            Button setDayButton = new Button
            {
                Text = "SET DAY",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Height = 30
            };
            telnetLayout.Controls.Add(setDayButton, 3, 0);

            // SET NIGHT�{�^��
            Button setNightButton = new Button
            {
                Text = "SET NIGHT",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Height = 30
            };
            telnetLayout.Controls.Add(setNightButton, 4, 0);

            // �ڑ��X�e�[�^�X���x��
            Label connectionStatusLabel = new Label
            {
                Text = "��ڑ���", // �������
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(5),
                ForeColor = Color.Red // ��ڑ����̕����F
            };
            telnetLayout.Controls.Add(connectionStatusLabel, 5, 0);

            // �{�^���̃N���b�N�C�x���g
            connectTelnetButton.Click += async (sender, e) =>
            {
                try
                {
                    if (isConnected)
                    {
                        telnetClientHelper?.Close();
                        isConnected = false;
                        connectionStatusLabel.Text = "��ڑ���";
                        connectionStatusLabel.ForeColor = Color.Red;
                        connectTelnetButton.Text = "Telnet�ڑ�";
                        AddLogEntry("Telnet�ڑ����������܂����B");
                        return;
                    }

                    string ipAddress = ipAddressTextBox.Text.Trim();
                    string username = "x1s";
                    string password = "x1s";

                    if (string.IsNullOrWhiteSpace(ipAddress))
                    {
                        MessageBox.Show("IP�A�h���X����͂��Ă��������B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    connectionStatusLabel.Text = "�ڑ���...";
                    connectionStatusLabel.ForeColor = Color.Blue;

                    telnetClientHelper = new TelnetClientHelper();

                    // Telnet�ڑ������݂�
                    TcpClient tcpClient = new TcpClient();
                    await tcpClient.ConnectAsync(ipAddress, 23);
                    NetworkStream stream = tcpClient.GetStream();
                    telnetClientHelper.InitializeStream(stream);

                    // ���O�C������
                    bool loginSuccess = await telnetClientHelper.LoginAsync(username, password);
                    if (loginSuccess)
                    {
                        isConnected = true;
                        connectionStatusLabel.Text = "�ڑ���";
                        connectionStatusLabel.ForeColor = Color.Green;
                        connectTelnetButton.Text = "Telnet�ڑ�����";
                        AddLogEntry($"Telnet�ڑ�����у��O�C���ɐ������܂���: {ipAddress}");

                        // �o�b�N���C�g�Ɠx���擾
                        var backlightResults = await telnetClientHelper.GetBacklightBrightnessForKeypads(structuredData);
                        foreach (var result in backlightResults)
                        {
                            AddLogEntry($"�f�o�C�X: {result.Key}, �A�N�e�B�u�Ɠx: {result.Value.Active}, �C���A�N�e�B�u�Ɠx: {result.Value.Inactive}");
                        }
                    }
                    else
                    {
                        throw new Exception("���O�C���Ɏ��s���܂����B");
                    }
                }
                catch (Exception ex)
                {
                    connectionStatusLabel.Text = "��ڑ���";
                    connectionStatusLabel.ForeColor = Color.Red;
                    AddLogEntry($"�G���[: {ex.Message}");
                }
            };

            getTimeButton.Click += async (sender, e) =>
            {
                if (isConnected)
                {
                    try
                    {
                        string getTimeCommand = "GETTIME";
                        string response = await telnetClientHelper.SendCommandAsync(getTimeCommand);
                        AddLogEntry($"���M: {getTimeCommand}");
                        AddLogEntry($"����: {response}");
                    }
                    catch (Exception ex)
                    {
                        AddLogEntry($"GETTIME �R�}���h�̎��s���ɃG���[���������܂���: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Telnet�ڑ����ł͂���܂���B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            setDayButton.Click += async (sender, e) =>
            {
                if (isConnected)
                {
                    try
                    {
                        string setDayCommand = "SETTIME,9";
                        string response = await telnetClientHelper.SendCommandAsync(setDayCommand);
                        AddLogEntry($"���M: {setDayCommand}");
                        AddLogEntry($"����: {response}");
                    }
                    catch (Exception ex)
                    {
                        AddLogEntry($"SET DAY �R�}���h�̎��s���ɃG���[���������܂���: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Telnet�ڑ����ł͂���܂���B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            setNightButton.Click += async (sender, e) =>
            {
                if (isConnected)
                {
                    try
                    {
                        string setNightCommand = "SETTIME,21";
                        string response = await telnetClientHelper.SendCommandAsync(setNightCommand);
                        AddLogEntry($"���M: {setNightCommand}");
                        AddLogEntry($"����: {response}");
                    }
                    catch (Exception ex)
                    {
                        AddLogEntry($"SET NIGHT �R�}���h�̎��s���ɃG���[���������܂���: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Telnet�ڑ����ł͂���܂���B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Telnet���C�A�E�g��z�u
            leftLayout.Controls.Add(telnetLayout, 0, 2);




            // Telnet���C�A�E�g��z�u
            leftLayout.Controls.Add(telnetLayout, 0, 2);

            // �L�[�p�b�h�\���G���A
            debugPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.WhiteSmoke // �w�i�F�𔖂��ݒ�
            };
            leftLayout.Controls.Add(debugPanel, 0, 3);

            mainSplitContainer.Panel1.Controls.Add(leftLayout);

            // �E���G���A: ���O�o��
            SplitContainer logSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel2, // �������Œ�
                SplitterDistance = (int)(this.Height * 0.9), // �㕔95%�����O�o�͕���
            };

            // �㕔���O�o�̓G���A
            GroupBox logGroupBox = new GroupBox
            {
                Text = "���O�o��",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            logListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                ScrollAlwaysVisible = true
            };

            // ���O�N���A�{�^��
            Button clearLogButton = new Button
            {
                Text = "���O�N���A",
                Dock = DockStyle.Top,
                Height = 30
            };
            clearLogButton.Click += (sender, e) =>
            {
                logListBox.Items.Clear();
            };

            // ���O�o�͕����ɃN���A�{�^���ƃ��X�g��ǉ�
            logGroupBox.Controls.Add(logListBox);
            logGroupBox.Controls.Add(clearLogButton);

            logSplitContainer.Panel1.Controls.Add(logGroupBox);

            // �����R�}���h���̓G���A
            GroupBox commandGroupBox = new GroupBox
            {
                Text = "�R�}���h����",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            TableLayoutPanel commandLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(5)
            };
            commandLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80)); // ���̓t�B�[���h
            commandLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); // ���M�{�^��

            TextBox commandInputBox = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "�R�}���h����͂��Ă�������"
            };

            Button sendCommandButton = new Button
            {
                Text = "���M",
                Dock = DockStyle.Fill,
                Height = 30 // Telnet�ڑ��{�^���Ɠ��������ɐݒ�
            };
            sendCommandButton.Click += async (sender, e) =>
            {
                string command = commandInputBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(command))
                {
                    MessageBox.Show("�R�}���h����͂��Ă��������B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!isConnected || telnetClientHelper == null)
                {
                    MessageBox.Show("Telnet�ڑ����m������Ă��܂���B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    // �R�}���h���M
                    AddLogEntry($"���M: {command}");
                    string response = await telnetClientHelper.SendCommandAsync(command);
                    AddLogEntry($"����: {response}");

                    // ���̓{�b�N�X���N���A
                    commandInputBox.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"�R�}���h���M���ɃG���[���������܂���: {ex.Message}", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // �R�}���h���͕��������C�A�E�g�ɒǉ�
            commandLayout.Controls.Add(commandInputBox, 0, 0);
            commandLayout.Controls.Add(sendCommandButton, 1, 0);

            commandGroupBox.Controls.Add(commandLayout);
            logSplitContainer.Panel2.Controls.Add(commandGroupBox);

            mainSplitContainer.Panel2.Controls.Add(logSplitContainer);

            this.Controls.Add(mainSplitContainer);

            // �t�H�[���T�C�Y�ύX����SplitterDistance�𒲐�
            this.Resize += (sender, e) =>
            {
                // ���E�������ێ�
                int desiredMainSplitDistance = (int)(this.Width * 0.7);
                int minDistance = mainSplitContainer.Panel1MinSize;
                int maxDistance = this.Width - mainSplitContainer.Panel2MinSize;
                mainSplitContainer.SplitterDistance = Math.Max(minDistance, Math.Min(desiredMainSplitDistance, maxDistance));

                // �㉺�������ێ�
                int desiredLogSplitDistance = (int)(mainSplitContainer.Panel2.Height * 0.9);
                logSplitContainer.SplitterDistance = desiredLogSplitDistance;
            };

            // �R�}���h���͕��������C�A�E�g�ɒǉ�
            commandLayout.Controls.Add(commandInputBox, 0, 0);
            commandLayout.Controls.Add(sendCommandButton, 1, 0);

            commandGroupBox.Controls.Add(commandLayout);
            logSplitContainer.Panel2.Controls.Add(commandGroupBox);

            mainSplitContainer.Panel2.Controls.Add(logSplitContainer);

            this.Controls.Add(mainSplitContainer);

            // �t�H�[���T�C�Y�ύX����SplitterDistance�𒲐�
            this.Resize += (sender, e) =>
            {
                int desiredDistance = (int)(this.Width * 0.7); // ��70%�A�E30%�ɒ���
                int minDistance = mainSplitContainer.Panel1MinSize;
                int maxDistance = this.Width - mainSplitContainer.Panel2MinSize;

                // SplitterDistance �̐�����l�����Đݒ�
                mainSplitContainer.SplitterDistance = Math.Max(minDistance, Math.Min(desiredDistance, maxDistance));
            };

            // �R�}���h���͕��������C�A�E�g�ɒǉ�
            commandLayout.Controls.Add(commandInputBox, 0, 0);
            commandLayout.Controls.Add(sendCommandButton, 1, 0);

            commandGroupBox.Controls.Add(commandLayout);
            logSplitContainer.Panel2.Controls.Add(commandGroupBox);

            mainSplitContainer.Panel2.Controls.Add(logSplitContainer);

            this.Controls.Add(mainSplitContainer);
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

            debugPanel.Controls.Clear();

            // FlowLayoutPanel�ŉ����тɃL�[�p�b�h��\��
            FlowLayoutPanel mainFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                WrapContents = false
            };

            // �f�o�C�X���O���[�v��
            var groupedDevices = structuredData[selectedRoomKey]
                .Where(device => device.Model.StartsWith("MWP-U") || device.Model.StartsWith("MWP-B")) // �L�[�p�b�h�̂݃t�B���^�����O
                .GroupBy(device => device.DeviceName.Split('(')[0]) // �f�o�C�X���̊�{�����ŃO���[�v��
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var group in groupedDevices)
            {
                // �O���[�v����\�����邽�߂̃��x��
                Label groupLabel = new Label
                {
                    Text = group.Key, // �O���[�v�̃f�o�C�X��
                    AutoSize = true,
                    Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold),
                    Padding = new Padding(5),
                    Margin = new Padding(10, 20, 10, 5),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                mainFlowPanel.Controls.Add(groupLabel);

                // �O���[�v���̃L�[�p�b�h�������тɔz�u
                FlowLayoutPanel groupFlowPanel = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.LeftToRight,
                    AutoSize = true,
                    WrapContents = true,
                    Padding = new Padding(10),
                    Margin = new Padding(10)
                };

                foreach (var device in group.Value)
                {
                    // �L�[�p�b�h���͂ޘg���쐬
                    Panel keypadContainer = new Panel
                    {
                        BackColor = Color.LightGray, // �D�F�̔w�i
                        Padding = new Padding(10), // �㉺���E�ɋϓ��ȗ]��
                        Margin = new Padding(5),   // �e�L�[�p�b�h�Ԃ̃X�y�[�X
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink
                    };

                    Keypad keypad = new Keypad(
                        device.Buttons.Count,
                        device.Buttons,
                        device.DColumns, // D����
                        AddLogEntry,
                        device.DeviceName,
                        device.ID, // �f�o�C�XID
                        () => isConnected, // �ڑ���Ԃ��m�F
                        (command) => telnetClientHelper.SendCommandAsync(command)) // Telnet�R�}���h���M
                    {
                        Name = group.Key, // �f�o�C�X�����L�[�Ƃ��Đݒ�i��: "SW2"�j
                        Margin = new Padding(5)
                    };

                    // �L�[�p�b�h��g���ɒǉ�
                    keypadContainer.Controls.Add(keypad);
                    groupFlowPanel.Controls.Add(keypadContainer);
                }

                mainFlowPanel.Controls.Add(groupFlowPanel);
            }

            debugPanel.Controls.Add(mainFlowPanel);
            debugPanel.Update();
        }

        private void AddLogEntry(string logMessage)
        {
            logListBox.Items.Add(logMessage);
            logListBox.TopIndex = logListBox.Items.Count - 1;
        }
    }
}