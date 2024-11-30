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
        private Dictionary<string, Dictionary<string, string>> additionalData; // �ǉ��f�[�^�p
        private ComboBox roomKeySelector;
        private DataGridView deviceTable;
        private Panel debugPanel;
        private ListBox logListBox;
        private TelnetClientHelper telnetClientHelper = null;
        private bool isConnected = false;
        private TabControl tabControl; // �N���X���x���Ő錾



        public MainForm()
        {
            InitializeComponent();
            InitializeResponseProcessing();

            if (SelectCsvFile(out string filePath))
            {
                // CSV�t�@�C��������
                (structuredData, additionalData) = CsvProcessor.ProcessCsvFile(filePath);

                // structuredData��n����TelnetClientHelper��������
                //telnetClientHelper = new TelnetClientHelper(structuredData);
                telnetClientHelper = new TelnetClientHelper(this, structuredData, additionalData);

                // �f�o�C�X�ύX�̊Ď����J�n
                MonitorDeviceChanges();

                InitializeUI();
            }
            else
            {
                MessageBox.Show("CSV�t�@�C�����I������܂���ł����B�A�v�����I�����܂��B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }

            // �^�u�R���g���[������ Lighting Status �^�u���擾
            TabPage lightingStatusTab = GetLightingStatusTab();

            // Lighting Status �^�u��������
            if (lightingStatusTab != null)
            {
                InitializeLightingStatusTab(lightingStatusTab);
            }
            else
            {
                MessageBox.Show("Lighting Status �^�u��������܂���ł����B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // �^�u�R���g���[������ Lighting Status �^�u���擾����w���p�[���\�b�h
        private TabPage GetLightingStatusTab()
        {
            foreach (Control control in this.Controls)
            {
                if (control is TabControl tabControl)
                {
                    foreach (TabPage tabPage in tabControl.TabPages)
                    {
                        if (tabPage.Text == "Lighting Status")
                        {
                            return tabPage;
                        }
                    }
                }
            }
            return null;
        }




        private System.Windows.Forms.Timer responseProcessingTimer;

        private void InitializeResponseProcessing()
        {
            responseProcessingTimer = new System.Windows.Forms.Timer
            {
                Interval =100
            };

            responseProcessingTimer.Tick += (sender, e) =>
            {
                if (telnetClientHelper != null && isConnected)
                {
                    telnetClientHelper.ProcessResponseBuffer(AddLogEntry);
                }
            };

            responseProcessingTimer.Start();
        }

        private void LogKeypadStates()
        {
            foreach (var room in structuredData.Keys)
            {
                AddLogEntry($"[INFO] ����: {room}");
                foreach (var device in structuredData[room])
                {
                    AddLogEntry($"[INFO] �f�o�C�X: {device.DeviceName}, ID: {device.ID}");

                    foreach (var buttonIndex in Enumerable.Range(1, device.Buttons.Count))
                    {
                        bool? buttonState = device.GetButtonState(buttonIndex);
                        string stateText = buttonState.HasValue
                            ? (buttonState.Value ? "�A�N�e�B�u" : "�C���A�N�e�B�u")
                            : "��ԕs��";

                        AddLogEntry($"  �{�^��: {device.Buttons[buttonIndex - 1]} (Index: {buttonIndex}), ���: {stateText}");
                    }

                    AddLogEntry($"[DEBUG] �S�{�^�����: {device.GetButtonStatesAsString()}");
                }
            }
        }




        private void InitializeComponent()
        {
            this.Text = "Room Keypad Manager";
            this.WindowState = FormWindowState.Maximized;

            // �N���X�t�B�[���h�� tabControl ��������
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // 1�ڂ̃^�u: Room Keypad Manager
            TabPage roomKeypadTab = new TabPage("Room Keypad Manager")
            {
                BackColor = Color.White
            };

            // 2�ڂ̃^�u: �Ɩ��̃X�e�[�^�X���
            TabPage lightingStatusTab = new TabPage("Lighting Status")
            {
                BackColor = Color.White
            };

            // 1�ڂ̃^�u�� UI ��z�u
            InitializeRoomKeypadManagerTab(roomKeypadTab);

            // �^�u��ǉ�
            tabControl.TabPages.Add(roomKeypadTab);
            tabControl.TabPages.Add(lightingStatusTab);

            // �t�H�[���� tabControl ��ǉ�
            this.Controls.Clear();
            this.Controls.Add(tabControl);
        }


        private void InitializeLightingStatusTab(TabPage lightingStatusTab)
        {
            if (additionalData == null || additionalData.Count == 0)
            {
                // �f�[�^���Ȃ��ꍇ�̃v���[�X�z���_
                Label placeholder = new Label
                {
                    Text = "�Ɩ��̃X�e�[�^�X�f�[�^������܂���B",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font(FontFamily.GenericSansSerif, 16, FontStyle.Bold)
                };

                lightingStatusTab.Controls.Add(placeholder);
                return;
            }

            // �S�̂��c�����ɕ��ׂ�Panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(10),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // �㕔: �`�F�b�N�{�b�N�X�G���A
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // ����: �f�[�^�e�[�u���G���A

            // �Z�N�V�����̑I���G���A
            var sectionFilterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5),
                BackColor = Color.White
            };

            // DataGridView���쐬
            var lightingTable = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // �J������ǉ� (�񕝂��w��)
            var sectionColumn = new DataGridViewTextBoxColumn
            {
                Name = "ColumnSection",
                HeaderText = "�Z�N�V����",
                FillWeight = 10 // ���̊����i10%�j
            };

            var keyColumn = new DataGridViewTextBoxColumn
            {
                Name = "ColumnKey",
                HeaderText = "�L�[",
                FillWeight = 50 // ���̊����i50%�j
            };

            var idColumn = new DataGridViewTextBoxColumn
            {
                Name = "ColumnID",
                HeaderText = "ID",
                FillWeight = 10 // ���̊����i10%�j
            };

            var valueColumn = new DataGridViewTextBoxColumn
            {
                Name = "ColumnValue",
                HeaderText = "�l",
                FillWeight = 30 // ���̊����i30%�j
            };

            lightingTable.Columns.AddRange(sectionColumn, keyColumn, idColumn, valueColumn);

            // �Z�N�V�������Ƃ̕\���Ǘ�
            Dictionary<string, bool> sectionVisibility = additionalData.Keys.ToDictionary(section => section, section => true);

            // �Z�N�V�����t�B���^�p�`�F�b�N�{�b�N�X���쐬
            foreach (var section in additionalData.Keys)
            {
                CheckBox sectionCheckBox = new CheckBox
                {
                    Text = section,
                    Checked = true,
                    AutoSize = true,
                    Margin = new Padding(5)
                };

                sectionCheckBox.CheckedChanged += (sender, e) =>
                {
                    sectionVisibility[section] = sectionCheckBox.Checked;
                    UpdateLightingTable();
                };

                sectionFilterPanel.Controls.Add(sectionCheckBox);
            }

            // �f�[�^���X�V���郍�W�b�N
            void UpdateLightingTable()
            {
                lightingTable.Rows.Clear(); // �����̍s���N���A

                foreach (var section in additionalData.Keys)
                {
                    if (!sectionVisibility[section]) continue; // ��\���̃Z�N�V�����̓X�L�b�v

                    foreach (var item in additionalData[section])
                    {
                        lightingTable.Rows.Add(section, item.Key, item.Value, ""); // �l��ɂ͋󔒂�}��
                    }
                }
            }

            // �����f�[�^��ǉ�
            UpdateLightingTable();

            // �R���|�[�l���g��z�u
            mainPanel.Controls.Add(sectionFilterPanel, 0, 0);
            mainPanel.Controls.Add(lightingTable, 0, 1);

            // �쐬����Panel���^�u�ɒǉ�
            lightingStatusTab.Controls.Clear();
            lightingStatusTab.Controls.Add(mainPanel);
        }



        private void InitializeRoomKeypadManagerTab(TabPage roomKeypadTab)
        {
            // SplitContainer�ŉ�ʂ����E�ɕ���
            SplitContainer mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = (int)(this.Width * 0.7),
                FixedPanel = FixedPanel.None
            };

            // �����G���A: 4����
            TableLayoutPanel leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5, // CSV�I���G���A��ǉ����邽��4��5�ɕύX
                Padding = new Padding(10)
            };
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // CSV�I���G���A
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40)); // CSV���
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 5));  // �f�o�b�O�쐬�{�^��
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 5));  // Telnet�ڑ��p
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // �L�[�p�b�h�\��

            // CSV�I���G���A
            TableLayoutPanel csvSelectionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(5),
                AutoSize = true
            };

            csvSelectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80)); // �A�h���X�\���pTextBox (80%��)
            csvSelectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));    // �Q�ƃ{�^�� (������)

            // CSV���݃A�h���X�\���pTextBox
            TextBox csvPathTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                PlaceholderText = "CSV�t�@�C���̃p�X��I�����Ă�������",
                Margin = new Padding(5)
            };

            // CSV�Q�ƃ{�^��
            Button browseCsvButton = new Button
            {
                Text = "�Q��",
                AutoSize = true,          // �T�C�Y���R���e���c�ɍ��킹��
                AutoSizeMode = AutoSizeMode.GrowAndShrink, // ����ɏ����������\
                Margin = new Padding(5),  // �{�^�����͂̃X�y�[�X
                Height = 25               // �{�^���̍�����1�s���ɒ���
            };

            // �{�^���̃N���b�N�C�x���g��CSV�t�@�C���I���_�C�A���O��\��
            browseCsvButton.Click += (sender, e) =>
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    Title = "CSV�t�@�C����I�����Ă�������"
                })
                {
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string selectedFilePath = openFileDialog.FileName;
                        csvPathTextBox.Text = selectedFilePath; // �I�������p�X��TextBox�ɕ\��

                        // CSV�f�[�^���ēǂݍ��݂���UI���X�V
                        (structuredData, additionalData) = CsvProcessor.ProcessCsvFile(selectedFilePath);
                        roomKeySelector.Items.Clear();
                        InitializeUI(); // �ēxUI��������
                        MessageBox.Show("CSV�t�@�C��������ɓǂݍ��܂�܂����B", "����", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };

            // CSV�I���G���A�ɃR���|�[�l���g��ǉ�
            csvSelectionLayout.Controls.Add(csvPathTextBox, 0, 0);
            csvSelectionLayout.Controls.Add(browseCsvButton, 1, 0);
            leftLayout.Controls.Add(csvSelectionLayout, 0, 0);


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

            leftLayout.Controls.Add(csvInfoLayout, 1, 0);

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
            TableLayoutPanel telnetLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = 1,
                Padding = new Padding(10)
            };
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // IP���̓t�B�[���h
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // �ڑ��{�^��
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // ���W�I�{�^���G���A
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));   // �X�e�[�^�X���x��
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // GETTIME�{�^��
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // SET DAY�{�^��
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // SET NIGHT�{�^��
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // ��Ԋm�F�{�^��


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
            telnetLayout.Controls.Add(getTimeButton, 4, 0);

            // SET DAY�{�^��
            Button setDayButton = new Button
            {
                Text = "SET DAY",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Height = 30
            };
            telnetLayout.Controls.Add(setDayButton, 5, 0);

            // SET NIGHT�{�^��
            Button setNightButton = new Button
            {
                Text = "SET NIGHT",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Height = 30
            };
            telnetLayout.Controls.Add(setNightButton, 6, 0);

            // �ڑ��X�e�[�^�X���x��
            Label connectionStatusLabel = new Label
            {
                Text = "��ڑ���",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(5),
                ForeColor = Color.Red
            };
            telnetLayout.Controls.Add(connectionStatusLabel, 3, 0);

            // ��Ԋm�F�{�^��
            Button checkStateButton = new Button
            {
                Text = "��Ԋm�F",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Height = 30
            };
            telnetLayout.Controls.Add(checkStateButton, 7, 0);

            // ���W�I�{�^���G���A
            FlowLayoutPanel radioPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(5)
            };

            RadioButton gcuRadioButton = new RadioButton
            {
                Text = "GCU",
                Checked = true, // �f�t�H���g�őI��
                AutoSize = true,
                Margin = new Padding(5)
            };

            RadioButton nwkRadioButton = new RadioButton
            {
                Text = "NWK",
                AutoSize = true,
                Margin = new Padding(5)
            };

            // ���W�I�{�^����ǉ�
            radioPanel.Controls.Add(gcuRadioButton);
            radioPanel.Controls.Add(nwkRadioButton);
            telnetLayout.Controls.Add(radioPanel, 2, 0);

            // �{�^���̃C�x���g�n���h�����őI����e���g�p�\�ɂ���
            connectTelnetButton.Click += (sender, e) =>
            {
                if (gcuRadioButton.Checked)
                {
                    AddLogEntry("�I�����ꂽ�ڑ��^�C�v: GCU");
                }
                else if (nwkRadioButton.Checked)
                {
                    AddLogEntry("�I�����ꂽ�ڑ��^�C�v: NWK");
                }
                else
                {
                    MessageBox.Show("�ڑ��^�C�v��I�����Ă��������B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            checkStateButton.Click += (sender, e) =>
            {
                LogKeypadStates(); // �L�[�p�b�h�̏�Ԃ����O�ɏo��
            };

            getTimeButton.Click += async (sender, e) =>
            {
                if (isConnected)
                {
                    try
                    {
                        string getTimeCommand = "GETTIME";
                        await telnetClientHelper.SendCommandAsync(getTimeCommand);
                        AddLogEntry($"���M: {getTimeCommand}");
                        //AddLogEntry($"����: {response}");
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
                        await telnetClientHelper.SendCommandAsync(setDayCommand);
                        AddLogEntry($"���M: {setDayCommand}");
                        //AddLogEntry($"����: {response}");
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
                        await telnetClientHelper.SendCommandAsync(setNightCommand);
                        AddLogEntry($"���M: {setNightCommand}");
                        //AddLogEntry($"����: {response}");
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

            connectTelnetButton.Click += async (sender, e) =>
            {
                try
                {
                    // �ڑ���������
                    if (isConnected)
                    {
                        telnetClientHelper?.StopListening();
                        telnetClientHelper?.Close();
                        isConnected = false;
                        connectionStatusLabel.Text = "��ڑ���";
                        connectionStatusLabel.ForeColor = Color.Red;
                        connectTelnetButton.Text = "Telnet�ڑ�";
                        AddLogEntry("Telnet�ڑ����������܂����B");
                        return;
                    }

                    // �ڑ����擾
                    string ipAddress = ipAddressTextBox.Text.Trim();
                    string username = "x1s";
                    string password = "x1s";
                    string nwk = "nwk";

                    if (string.IsNullOrWhiteSpace(ipAddress))
                    {
                        MessageBox.Show("IP�A�h���X����͂��Ă��������B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // �ڑ��^�C�v�̔���
                    bool nwk_check = nwkRadioButton.Checked; // NWK���I������Ă���ꍇ��true�AMyRoom�̏ꍇ��false

                    // �ڑ��X�e�[�^�X���X�V
                    connectionStatusLabel.Text = "�ڑ���...";
                    connectionStatusLabel.ForeColor = Color.Blue;

                    // Telnet�ڑ�������
                    //telnetClientHelper = new TelnetClientHelper(structuredData);
                    telnetClientHelper = new TelnetClientHelper(this, structuredData, additionalData);

                    TcpClient tcpClient = new TcpClient();
                    await tcpClient.ConnectAsync(ipAddress, 23);
                    NetworkStream stream = tcpClient.GetStream();
                    telnetClientHelper.InitializeStream(stream);

                    // ���O�C������
                    bool loginSuccess = await telnetClientHelper.LoginAsync(username, password, nwk, nwk_check);
                    if (loginSuccess)
                    {
                        isConnected = true;
                        connectionStatusLabel.Text = "�ڑ���";
                        connectionStatusLabel.ForeColor = Color.Green;
                        connectTelnetButton.Text = "Telnet�ڑ�����";
                        AddLogEntry($"Telnet�ڑ�����у��O�C���ɐ������܂���: {ipAddress}");

                        // ���X�j���O�J�n
                        telnetClientHelper.StartListening();

                        // �m�F�R�}���h�𑗐M
                        string getTimeCommand = "GETTIME";
                        await telnetClientHelper.SendCommandAsync(getTimeCommand);
                        AddLogEntry($"GETTIME�R�}���h�𑗐M���܂����B");

                        //await telnetClientHelper.SendBacklightBrightnessCommandsForKeypads(structuredData, AddLogEntry);
                        AddLogEntry("�L�[�p�b�h�̃o�b�N���C�g�m�F�R�}���h�𑗐M���܂����B");
                    }
                    else
                    {
                        throw new Exception("���O�C���Ɏ��s���܂����B");
                    }
                }
                catch (Exception ex)
                {
                    // �G���[����
                    connectionStatusLabel.Text = "��ڑ���";
                    connectionStatusLabel.ForeColor = Color.Red;
                    AddLogEntry($"�G���[: {ex.Message}");
                }
            };


            leftLayout.Controls.Add(telnetLayout, 0, 2);

            // �L�[�p�b�h�\���G���A
            debugPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.WhiteSmoke
            };
            leftLayout.Controls.Add(debugPanel, 0, 3);

            mainSplitContainer.Panel1.Controls.Add(leftLayout);

            // �E���G���A: ���O�o��
            SplitContainer logSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel2,
                SplitterDistance = (int)(this.Height * 0.8)
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
            commandLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80));
            commandLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            TextBox commandInputBox = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "�R�}���h����͂��Ă�������"
            };

            Button sendCommandButton = new Button
            {
                Text = "���M",
                Dock = DockStyle.Fill,
                Height = 30
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
                    AddLogEntry($"���M: {command}");
                    string response = await telnetClientHelper.SendCommandAsync(command);
                    AddLogEntry($"����: {response}");
                    commandInputBox.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"�R�}���h���M���ɃG���[���������܂���: {ex.Message}", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            commandLayout.Controls.Add(commandInputBox, 0, 0);
            commandLayout.Controls.Add(sendCommandButton, 1, 0);
            commandGroupBox.Controls.Add(commandLayout);
            logSplitContainer.Panel2.Controls.Add(commandGroupBox);

            mainSplitContainer.Panel2.Controls.Add(logSplitContainer);

            roomKeypadTab.Controls.Add(mainSplitContainer);
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

                    // �L�[�p�b�h�̃C���X�^���X���쐬
                    Keypad keypad = new Keypad(
                        device.DeviceName,
                        device.ID,
                        device.Buttons,
                        device.DColumns,
                        () => isConnected,
                        (buttonIndex) => device.GetButtonState(buttonIndex),
                        (id) => structuredData[selectedRoomKey].First(d => d.ID == id).ActiveBrightness,
                        (id) => structuredData[selectedRoomKey].First(d => d.ID == id).InactiveBrightness,
                        AddLogEntry,
                        (command) => telnetClientHelper.SendCommandAsync(command))
                    {
                        Name = device.ID, // DeviceID �� Name �ɐݒ�
                        Margin = new Padding(5)
                    };

                    // ���O�ɒǉ������L�^
                    //AddLogEntry($"[DEBUG] Adding Keypad: {keypad.DeviceName}, ID: {keypad.DeviceID}, Name: {keypad.Name}");

                    // �L�[�p�b�h��g���ɒǉ�
                    keypadContainer.Controls.Add(keypad);
                    groupFlowPanel.Controls.Add(keypadContainer);
                }

                mainFlowPanel.Controls.Add(groupFlowPanel);
            }

            // `debugPanel` �ɒǉ�
            debugPanel.Controls.Add(mainFlowPanel);

            // �m�F�p���O���o��
            AddLogEntry($"[DEBUG] Finished adding keypads to debugPanel. Total groups: {groupedDevices.Count}");
            foreach (Control control in debugPanel.Controls)
            {
                AddLogEntry($"[DEBUG] Control in debugPanel: {control.GetType().Name}");
            }

            debugPanel.Update();
        }



        private void AddLogEntry(string logMessage)
        {
            logListBox.Items.Add(logMessage);
            logListBox.TopIndex = logListBox.Items.Count - 1;
        }

        private bool? GetButtonState(string deviceID, int buttonIndex)
        {
            if (structuredData != null)
            {
                // �Ή�����f�o�C�X������
                var device = structuredData.Values
                    .SelectMany(devices => devices)
                    .FirstOrDefault(d => d.ID == deviceID);

                // �{�^����Ԃ��擾�i���̃��W�b�N�B���ۂɂ̓{�^����Ԃ�ێ�����f�[�^�\�����K�v�j
                return device?.Buttons.Count > buttonIndex
                    ? (bool?)true // ���ɂ��ׂẴ{�^�����A�N�e�B�u
                    : (bool?)false;
            }
            return null;
        }

        private int GetActiveBrightness(string deviceID)
        {
            if (structuredData != null)
            {
                var device = structuredData.Values
                    .SelectMany(devices => devices)
                    .FirstOrDefault(d => d.ID == deviceID);
                return device?.ActiveBrightness ?? 0;
            }
            return 0;
        }

        private int GetInactiveBrightness(string deviceID)
        {
            if (structuredData != null)
            {
                var device = structuredData.Values
                    .SelectMany(devices => devices)
                    .FirstOrDefault(d => d.ID == deviceID);
                return device?.InactiveBrightness ?? 0;
            }
            return 0;
        }

        //public Keypad FindKeypadByDeviceID(string deviceID)
        //{
        //    foreach (Control control in debugPanel.Controls)
        //    {
        //        if (control is FlowLayoutPanel layoutPanel)
        //        {
        //            foreach (Control childControl in layoutPanel.Controls)
        //            {
        //                if (childControl is Keypad keypad && keypad.DeviceID == deviceID)
        //                {
        //                    return keypad;
        //                }
        //            }
        //        }
        //    }

        //    AddLogEntry($"[ERROR] DeviceID: {deviceID} �ɑΉ����� Keypad ��������܂���B");
        //    return null;
        //}




        // MainForm.cs �ɋL�ڂ���֐�
        // 2�ڂ̃^�u���X�V����֐�
        public void UpdateLightingStatusTabBrightness(string id, float brightness)
        {
            if (tabControl == null)
            {
                Console.WriteLine("[ERROR] tabControl �� null �ł��B");
                return;
            }

            foreach (TabPage tabPage in tabControl.TabPages)
            {
                if (tabPage.Text == "Lighting Status")
                {
                    foreach (Control control in tabPage.Controls)
                    {
                        if (control is TableLayoutPanel tableLayoutPanel)
                        {
                            foreach (Control childControl in tableLayoutPanel.Controls)
                            {
                                if (childControl is DataGridView lightingTable)
                                {
                                    foreach (DataGridViewRow row in lightingTable.Rows)
                                    {
                                        if (row.Cells["ColumnID"].Value == null)
                                        {
                                            Console.WriteLine($"[DEBUG] ID ��̒l�� null �ł��B�s: {row.Index}");
                                            continue;
                                        }

                                        if (row.Cells["ColumnID"].Value.ToString() == id)
                                        {
                                            row.Cells["ColumnValue"].Value = $"{brightness}%";
                                            Console.WriteLine($"[INFO] ID: {id} �̍s���X�V���܂����B");
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"[DEBUG] ID: {id} �Ɉ�v����s��������܂���ł����B");
        }

        private void MonitorDeviceChanges()
        {
            foreach (var roomDevices in structuredData.Values)
            {
                foreach (var device in roomDevices)
                {
                    // �{�^���̏�Ԃ��ω������Ƃ��̃C�x���g���T�u�X�N���C�u
                    device.ButtonStateChanged += (deviceID, buttonIndex) =>
                    {
                        //AddLogEntry($"[DEBUG] UpdateKeypadButton called with DeviceID: {deviceID}, ButtonIndex: {buttonIndex}");

                        UpdateKeypadButton(deviceID, buttonIndex);
                    };
                }
            }
        }

        /// <summary>
        /// �w�肳�ꂽ�f�o�C�X�̃{�^�����X�V
        /// </summary>
        /// <param name="deviceID">�f�o�C�XID</param>
        /// <param name="buttonIndex">�{�^���ԍ�</param>
        private void UpdateKeypadButton(string deviceID, int buttonIndex)
        {
            if (debugPanel.InvokeRequired)
            {
                debugPanel.Invoke(new Action(() => UpdateKeypadButton(deviceID, buttonIndex)));
                return;
            }

            //AddLogEntry($"[DEBUG] Searching for Keypad with DeviceID: {deviceID}");

            // �ċA�I�� debugPanel ���̂��ׂẴR���g���[����T��
            bool found = false;
            foreach (Control control in debugPanel.Controls)
            {
                found = SearchAndUpdateKeypad(control, deviceID, buttonIndex);
                if (found) break;
            }

            if (!found)
            {
                AddLogEntry($"[WARN] �f�o�C�X {deviceID} �ɑΉ�����L�[�p�b�h��������܂���ł����B");
            }
        }

        private bool SearchAndUpdateKeypad(Control control, string deviceID, int buttonIndex)
        {
            //AddLogEntry($"[DEBUG] Control: {control.Name}, Type: {control.GetType()}");

            // Keypad �̏ꍇ�A�������`�F�b�N
            if (control is Keypad keypad)
            {
                //AddLogEntry($"[DEBUG] Found Keypad: {keypad.DeviceName}, ID: {keypad.DeviceID}");
                if (keypad.DeviceID == deviceID)
                {
                    //AddLogEntry($"[INFO] Updating Keypad: {keypad.DeviceName}, ID: {deviceID}");
                    keypad.UpdateButtonState(buttonIndex);
                    return true;
                }
            }

            // �q�R���g���[�������݂���ꍇ�A�ċA�I�ɒT��
            foreach (Control child in control.Controls)
            {
                if (SearchAndUpdateKeypad(child, deviceID, buttonIndex))
                {
                    return true;
                }
            }

            return false;
        }


    }

}
