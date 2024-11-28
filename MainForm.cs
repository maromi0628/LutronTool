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
        private Dictionary<string, Dictionary<string, string>> additionalData; // 追加データ用
        private ComboBox roomKeySelector;
        private DataGridView deviceTable;
        private Panel debugPanel;
        private ListBox logListBox;
        private TelnetClientHelper telnetClientHelper = null;
        private bool isConnected = false;


        public MainForm()
        {
            InitializeComponent();
            InitializeResponseProcessing();

            if (SelectCsvFile(out string filePath))
            {
                //var result = CsvProcessor.ProcessCsvFile(filePath);
                //structuredData = result.Item1;
                //additionalData = result.Item2;
                (structuredData, additionalData) = CsvProcessor.ProcessCsvFile(filePath);

                // structuredDataを渡してTelnetClientHelperを初期化
                telnetClientHelper = new TelnetClientHelper(structuredData);

                InitializeUI();
            }
            else
            {
                MessageBox.Show("CSVファイルが選択されませんでした。アプリを終了します。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }

            // Lighting Status タブを初期化
            InitializeLightingStatusTab();

        }



        private System.Windows.Forms.Timer responseProcessingTimer;

        private void InitializeResponseProcessing()
        {
            responseProcessingTimer = new System.Windows.Forms.Timer
            {
                Interval = 50
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
                AddLogEntry($"[INFO] 部屋: {room}");
                foreach (var device in structuredData[room])
                {
                    AddLogEntry($"[INFO] デバイス: {device.DeviceName}, ID: {device.ID}");

                    foreach (var buttonIndex in Enumerable.Range(1, device.Buttons.Count))
                    {
                        bool? buttonState = device.GetButtonState(buttonIndex);
                        string stateText = buttonState.HasValue
                            ? (buttonState.Value ? "アクティブ" : "インアクティブ")
                            : "状態不明";

                        AddLogEntry($"  ボタン: {device.Buttons[buttonIndex - 1]} (Index: {buttonIndex}), 状態: {stateText}");
                    }

                    AddLogEntry($"[DEBUG] 全ボタン状態: {device.GetButtonStatesAsString()}");
                }
            }
        }




        private void InitializeComponent()
        {
            this.Text = "Room Keypad Manager";
            this.WindowState = FormWindowState.Maximized;

            // タブコントロールの作成
            TabControl tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // 1つ目のタブ: Room Keypad Manager
            TabPage roomKeypadTab = new TabPage("Room Keypad Manager")
            {
                BackColor = Color.White
            };

            // 2つ目のタブ: 照明のステータス画面（空白）
            TabPage lightingStatusTab = new TabPage("Lighting Status")
            {
                BackColor = Color.White
            };

            // 1つ目のタブに現在のUIを配置
            InitializeRoomKeypadManagerTab(roomKeypadTab);

            // 2つ目のタブは空白の状態で設定
            //InitializeLightingStatusTab(lightingStatusTab);

            // タブコントロールにタブを追加
            tabControl.TabPages.Add(roomKeypadTab);
            tabControl.TabPages.Add(lightingStatusTab);

            // メインフォームにタブコントロールを追加
            this.Controls.Clear(); // 他のコントロールを削除してリセット
            this.Controls.Add(tabControl); // タブコントロールをメインに追加
        }

        private void InitializeLightingStatusTab()
        {
            // TabControl を取得
            var tabControl = (TabControl)this.Controls[0];

            // "Lighting Status" タブが既に存在するか確認し、あれば削除
            var existingTab = tabControl.TabPages.Cast<TabPage>().FirstOrDefault(tab => tab.Text == "Lighting Status");
            if (existingTab != null)
            {
                tabControl.TabPages.Remove(existingTab);
            }

            TabPage lightingStatusTab = new TabPage("Lighting Status")
            {
                BackColor = Color.White
            };

            if (additionalData == null || additionalData.Count == 0)
            {
                Label placeholder = new Label
                {
                    Text = "照明のステータスデータがありません。",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font(FontFamily.GenericSansSerif, 16, FontStyle.Bold)
                };

                lightingStatusTab.Controls.Add(placeholder);
            }
            else
            {
                // スクロール可能な Panel を作成
                Panel scrollablePanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true, // スクロールを有効化
                    BackColor = Color.White
                };

                TableLayoutPanel lightingTable = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 2,
                    AutoSize = true, // 自動サイズ設定
                    Padding = new Padding(10),
                    BackColor = Color.White
                };

                lightingTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                lightingTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

                foreach (var section in additionalData.Keys)
                {
                    Label sectionLabel = new Label
                    {
                        Text = section,
                        AutoSize = true,
                        Font = new Font(FontFamily.GenericSansSerif, 14, FontStyle.Bold),
                        Margin = new Padding(10, 20, 10, 5),
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    lightingTable.Controls.Add(sectionLabel);
                    lightingTable.SetColumnSpan(sectionLabel, 2);

                    foreach (var item in additionalData[section])
                    {
                        Label keyLabel = new Label
                        {
                            Text = item.Key,
                            AutoSize = true,
                            Font = new Font(FontFamily.GenericSansSerif, 12),
                            Margin = new Padding(5)
                        };

                        Label valueLabel = new Label
                        {
                            Text = item.Value,
                            AutoSize = true,
                            Font = new Font(FontFamily.GenericSansSerif, 12),
                            Margin = new Padding(5)
                        };

                        lightingTable.Controls.Add(keyLabel);
                        lightingTable.Controls.Add(valueLabel);
                    }
                }

                // TableLayoutPanel をスクロール可能な Panel に追加
                scrollablePanel.Controls.Add(lightingTable);

                // スクロール可能 Panel を TabPage に追加
                lightingStatusTab.Controls.Add(scrollablePanel);
            }

            // TabPage を追加
            tabControl.TabPages.Add(lightingStatusTab);
        }




        private void InitializeRoomKeypadManagerTab(TabPage roomKeypadTab)
        {
            // SplitContainerで画面を左右に分割
            SplitContainer mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = (int)(this.Width * 0.7),
                FixedPanel = FixedPanel.None
            };

            // 左側エリア: 4分割
            TableLayoutPanel leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10)
            };
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40)); // CSV情報
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 5));  // デバッグ作成ボタン
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 5));  // Telnet接続用
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // キーパッド表示

            // CSV情報エリア
            TableLayoutPanel csvInfoLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            csvInfoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 部屋選択ラベル
            csvInfoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // ComboBox
            csvInfoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // DataGridView

            Label roomKeyLabel = new Label
            {
                Text = "部屋を選択してください:",
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
            deviceTable.Columns.Add("ColumnDeviceName", "デバイス名");
            deviceTable.Columns.Add("ColumnModel", "モデル");
            deviceTable.Columns.Add("ColumnID", "ID");
            deviceTable.Columns.Add("ColumnButtons", "ボタン情報");
            csvInfoLayout.Controls.Add(deviceTable, 0, 2);

            leftLayout.Controls.Add(csvInfoLayout, 0, 0);

            // デバッグ環境作成ボタン
            Button createDebugEnvironmentButton = new Button
            {
                Text = "デバッグ環境作成",
                Dock = DockStyle.Fill,
                Height = 40
            };
            createDebugEnvironmentButton.Click += CreateDebugEnvironmentButton_Click;
            leftLayout.Controls.Add(createDebugEnvironmentButton, 0, 1);

            // Telnet接続用エリア
            TableLayoutPanel telnetLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 1,
                Padding = new Padding(10)
            };
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // IP入力フィールド
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // 接続ボタン
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // GETTIMEボタン
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // SET DAYボタン
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // SET NIGHTボタン
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));   // ステータスラベル
            telnetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // 状態確認ボタン

            // IP入力フィールド
            TextBox ipAddressTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "IPアドレスを入力してください",
                Margin = new Padding(5)
            };
            telnetLayout.Controls.Add(ipAddressTextBox, 0, 0);

            // 接続ボタン
            Button connectTelnetButton = new Button
            {
                Text = "Telnet接続",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Height = 30
            };
            telnetLayout.Controls.Add(connectTelnetButton, 1, 0);

            // GETTIMEボタン
            Button getTimeButton = new Button
            {
                Text = "GETTIME",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Height = 30
            };
            telnetLayout.Controls.Add(getTimeButton, 2, 0);

            // SET DAYボタン
            Button setDayButton = new Button
            {
                Text = "SET DAY",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Height = 30
            };
            telnetLayout.Controls.Add(setDayButton, 3, 0);

            // SET NIGHTボタン
            Button setNightButton = new Button
            {
                Text = "SET NIGHT",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Height = 30
            };
            telnetLayout.Controls.Add(setNightButton, 4, 0);

            // 接続ステータスラベル
            Label connectionStatusLabel = new Label
            {
                Text = "非接続中",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(5),
                ForeColor = Color.Red
            };
            telnetLayout.Controls.Add(connectionStatusLabel, 5, 0);

            // 状態確認ボタン
            Button checkStateButton = new Button
            {
                Text = "状態確認",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Height = 30
            };
            telnetLayout.Controls.Add(checkStateButton, 6, 0);

            checkStateButton.Click += (sender, e) =>
            {
                LogKeypadStates(); // キーパッドの状態をログに出力
            };

            getTimeButton.Click += async (sender, e) =>
            {
                if (isConnected)
                {
                    try
                    {
                        string getTimeCommand = "GETTIME";
                        await telnetClientHelper.SendCommandAsync(getTimeCommand);
                        AddLogEntry($"送信: {getTimeCommand}");
                        //AddLogEntry($"応答: {response}");
                    }
                    catch (Exception ex)
                    {
                        AddLogEntry($"GETTIME コマンドの実行中にエラーが発生しました: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Telnet接続中ではありません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        AddLogEntry($"送信: {setDayCommand}");
                        //AddLogEntry($"応答: {response}");
                    }
                    catch (Exception ex)
                    {
                        AddLogEntry($"SET DAY コマンドの実行中にエラーが発生しました: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Telnet接続中ではありません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        AddLogEntry($"送信: {setNightCommand}");
                        //AddLogEntry($"応答: {response}");
                    }
                    catch (Exception ex)
                    {
                        AddLogEntry($"SET NIGHT コマンドの実行中にエラーが発生しました: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Telnet接続中ではありません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            connectTelnetButton.Click += async (sender, e) =>
            {
                try
                {
                    // 接続解除処理
                    if (isConnected)
                    {
                        telnetClientHelper?.StopListening();
                        telnetClientHelper?.Close();
                        isConnected = false;
                        connectionStatusLabel.Text = "非接続中";
                        connectionStatusLabel.ForeColor = Color.Red;
                        connectTelnetButton.Text = "Telnet接続";
                        AddLogEntry("Telnet接続を解除しました。");
                        return;
                    }

                    // 接続情報取得
                    string ipAddress = ipAddressTextBox.Text.Trim();
                    string username = "x1s";
                    string password = "x1s";

                    if (string.IsNullOrWhiteSpace(ipAddress))
                    {
                        MessageBox.Show("IPアドレスを入力してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // 接続ステータスを更新
                    connectionStatusLabel.Text = "接続中...";
                    connectionStatusLabel.ForeColor = Color.Blue;

                    // Telnet接続初期化
                    telnetClientHelper = new TelnetClientHelper(structuredData);

                    TcpClient tcpClient = new TcpClient();
                    await tcpClient.ConnectAsync(ipAddress, 23);
                    NetworkStream stream = tcpClient.GetStream();
                    telnetClientHelper.InitializeStream(stream);

                    // ログイン処理
                    bool loginSuccess = await telnetClientHelper.LoginAsync(username, password);
                    if (loginSuccess)
                    {
                        isConnected = true;
                        connectionStatusLabel.Text = "接続中";
                        connectionStatusLabel.ForeColor = Color.Green;
                        connectTelnetButton.Text = "Telnet接続解除";
                        AddLogEntry($"Telnet接続およびログインに成功しました: {ipAddress}");

                        // リスニング開始
                        telnetClientHelper.StartListening();

                        // 確認コマンドを送信
                        string getTimeCommand = "GETTIME";
                        await telnetClientHelper.SendCommandAsync(getTimeCommand);
                        AddLogEntry($"GETTIMEコマンドを送信しました。");

                        await telnetClientHelper.SendBacklightBrightnessCommandsForKeypads(structuredData, AddLogEntry);
                        AddLogEntry("キーパッドのバックライト確認コマンドを送信しました。");
                    }
                    else
                    {
                        throw new Exception("ログインに失敗しました。");
                    }
                }
                catch (Exception ex)
                {
                    // エラー処理
                    connectionStatusLabel.Text = "非接続中";
                    connectionStatusLabel.ForeColor = Color.Red;
                    AddLogEntry($"エラー: {ex.Message}");
                }
            };


            leftLayout.Controls.Add(telnetLayout, 0, 2);

            // キーパッド表示エリア
            debugPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.WhiteSmoke
            };
            leftLayout.Controls.Add(debugPanel, 0, 3);

            mainSplitContainer.Panel1.Controls.Add(leftLayout);

            // 右側エリア: ログ出力
            SplitContainer logSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel2,
                SplitterDistance = (int)(this.Height * 0.9)
            };

            // 上部ログ出力エリア
            GroupBox logGroupBox = new GroupBox
            {
                Text = "ログ出力",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            logListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                ScrollAlwaysVisible = true
            };

            // ログクリアボタン
            Button clearLogButton = new Button
            {
                Text = "ログクリア",
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

            // 下部コマンド入力エリア
            GroupBox commandGroupBox = new GroupBox
            {
                Text = "コマンド入力",
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
                PlaceholderText = "コマンドを入力してください"
            };

            Button sendCommandButton = new Button
            {
                Text = "送信",
                Dock = DockStyle.Fill,
                Height = 30
            };
            sendCommandButton.Click += async (sender, e) =>
            {
                string command = commandInputBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(command))
                {
                    MessageBox.Show("コマンドを入力してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!isConnected || telnetClientHelper == null)
                {
                    MessageBox.Show("Telnet接続が確立されていません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    AddLogEntry($"送信: {command}");
                    string response = await telnetClientHelper.SendCommandAsync(command);
                    AddLogEntry($"応答: {response}");
                    commandInputBox.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"コマンド送信中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            debugPanel.Controls.Clear();

            // FlowLayoutPanelで横並びにキーパッドを表示
            FlowLayoutPanel mainFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                WrapContents = false
            };

            // デバイスをグループ化
            var groupedDevices = structuredData[selectedRoomKey]
                .Where(device => device.Model.StartsWith("MWP-U") || device.Model.StartsWith("MWP-B")) // キーパッドのみフィルタリング
                .GroupBy(device => device.DeviceName.Split('(')[0]) // デバイス名の基本部分でグループ化
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var group in groupedDevices)
            {
                // グループ名を表示するためのラベル
                Label groupLabel = new Label
                {
                    Text = group.Key, // グループのデバイス名
                    AutoSize = true,
                    Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold),
                    Padding = new Padding(5),
                    Margin = new Padding(10, 20, 10, 5),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                mainFlowPanel.Controls.Add(groupLabel);

                // グループ内のキーパッドを横並びに配置
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
                    // キーパッドを囲む枠を作成
                    Panel keypadContainer = new Panel
                    {
                        BackColor = Color.LightGray, // 灰色の背景
                        Padding = new Padding(10), // 上下左右に均等な余白
                        Margin = new Padding(5),   // 各キーパッド間のスペース
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink
                    };

                    Keypad keypad = new Keypad(
                        device.DeviceName, // デバイス名
                        device.ID,         // デバイスID
                        device.Buttons,    // ボタン名リスト (CSV の F 列データ)
                        device.DColumns,   // D列情報
                        () => isConnected, // Telnet接続確認
                        (buttonIndex) => device.GetButtonState(buttonIndex), // ボタン状態取得
                        (id) => structuredData[selectedRoomKey].First(d => d.ID == id).ActiveBrightness, // アクティブ照度取得
                        (id) => structuredData[selectedRoomKey].First(d => d.ID == id).InactiveBrightness, // インアクティブ照度取得
                        AddLogEntry, // ログ出力
                        (command) => telnetClientHelper.SendCommandAsync(command)) // コマンド送信
                    {
                        Name = group.Key, // デバイス名をキーとして設定（例: "SW2"）
                        Margin = new Padding(5)
                    };

                    // キーパッドを枠内に追加
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

        private bool? GetButtonState(string deviceID, int buttonIndex)
        {
            if (structuredData != null)
            {
                // 対応するデバイスを検索
                var device = structuredData.Values
                    .SelectMany(devices => devices)
                    .FirstOrDefault(d => d.ID == deviceID);

                // ボタン状態を取得（仮のロジック。実際にはボタン状態を保持するデータ構造が必要）
                return device?.Buttons.Count > buttonIndex
                    ? (bool?)true // 仮にすべてのボタンがアクティブ
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

        //    AddLogEntry($"[ERROR] DeviceID: {deviceID} に対応する Keypad が見つかりません。");
        //    return null;
        //}



    }
}
