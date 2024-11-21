using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RoomKeypadManager
{
    public class Keypad : UserControl
    {
        public string DeviceName { get; set; } // デバイス名を保持
        private List<string> dColumns; // D列情報
        private string deviceID; // デバイスID
        private Func<bool> isTelnetConnected; // Telnet接続確認用デリゲート
        private Action<string> sendTelnetCommand; // Telnetコマンド送信用デリゲート

        public Keypad(
            int buttonCount,
            List<string> buttonNames,
            List<string> dColumnValues,
            Action<string> addLog,
            string deviceName,
            string deviceID,
            Func<bool> isTelnetConnected,
            Action<string> sendTelnetCommand)
        {
            this.DeviceName = deviceName;
            this.deviceID = deviceID;
            this.dColumns = dColumnValues;
            this.isTelnetConnected = isTelnetConnected;
            this.sendTelnetCommand = sendTelnetCommand;

            // FlowLayoutPanelの設定
            FlowLayoutPanel layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, // 上詰めで配置
                AutoSize = true, // コンテンツに合わせてサイズ調整
                AutoSizeMode = AutoSizeMode.GrowAndShrink, // サイズが自動で拡張
                FlowDirection = FlowDirection.TopDown, // 縦方向にボタンを配置
                WrapContents = false // 横方向に折り返しを防ぐ
            };

            // ボタンを追加
            for (int i = 0; i < buttonCount; i++)
            {
                string buttonName = buttonNames[i];
                string dColumnValue = i < dColumns.Count ? dColumns[i] : "N/A"; // D列情報が不足する場合に備える
                string dColumnIndex = dColumnValue.Replace("Button ", ""); // "Button "を除去して番号を取得

                Button button = new Button
                {
                    Text = buttonName,
                    Size = new Size(150, 50), // ボタンのサイズを調整
                    Margin = new Padding(10) // ボタン間の余白
                };

                // ボタンが押されたとき (press)
                button.MouseDown += (sender, e) =>
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string logMessage = $"[{timestamp}] Device: {DeviceName}, Button: {button.Text} ({dColumnValue}), press";
                    addLog(logMessage);

                    // Telnetでコマンド送信
                    if (isTelnetConnected())
                    {
                        string telnetCommand = $"#DEVICE,{deviceID},{dColumnIndex},3";
                        sendTelnetCommand(telnetCommand);
                        addLog($"[Telnet Command Sent] {telnetCommand}");
                    }
                };

                // ボタンが離されたとき (release)
                button.MouseUp += (sender, e) =>
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string logMessage = $"[{timestamp}] Device: {DeviceName}, Button: {button.Text} ({dColumnValue}), release";
                    addLog(logMessage);

                    // Telnetでコマンド送信
                    if (isTelnetConnected())
                    {
                        string telnetCommand = $"#DEVICE,{deviceID},{dColumnIndex},4";
                        sendTelnetCommand(telnetCommand);
                        addLog($"[Telnet Command Sent] {telnetCommand}");
                    }
                };

                layout.Controls.Add(button);
            }

            // Keypad全体の設定
            this.AutoSize = true; // コンテンツに合わせてサイズ調整
            this.Controls.Add(layout); // FlowLayoutPanelを追加
        }
    }
}