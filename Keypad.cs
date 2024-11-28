﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RoomKeypadManager
{
    public class Keypad : UserControl
    {
        public string DeviceName { get; set; } // デバイス名を保持
        private List<string> buttonNames; // ボタン名リスト
        private List<string> dColumns; // D列情報
        private string deviceID; // デバイスID
        private Func<bool> isTelnetConnected; // Telnet接続確認用デリゲート
        private Func<int, bool?> getButtonState; // ボタン状態取得用デリゲート
        private Func<string, int> getActiveBrightness; // アクティブ照度取得用デリゲート
        private Func<string, int> getInactiveBrightness; // インアクティブ照度取得用デリゲート
        private Action<string> sendTelnetCommand; // Telnetコマンド送信用デリゲート
        private Action<string> logAction; // ログ出力用デリゲート
        private Dictionary<int, Button> buttonControls = new Dictionary<int, Button>(); // ボタンコントロールの管理

        public Keypad(
            string deviceName,
            string deviceID,
            List<string> buttonNames,
            List<string> dColumns,
            Func<bool> isTelnetConnected,
            Func<int, bool?> getButtonState,
            Func<string, int> getActiveBrightness,
            Func<string, int> getInactiveBrightness,
            Action<string> logAction,
            Action<string> sendTelnetCommand) // コマンド送信用
        {
            this.DeviceName = deviceName;
            this.deviceID = deviceID;
            this.buttonNames = buttonNames;
            this.dColumns = dColumns;
            this.isTelnetConnected = isTelnetConnected;
            this.getButtonState = getButtonState;
            this.getActiveBrightness = getActiveBrightness;
            this.getInactiveBrightness = getInactiveBrightness;
            this.logAction = logAction;
            this.sendTelnetCommand = sendTelnetCommand;

            InitializeKeypad();
        }


        private void InitializeKeypad()
        {
            // キーパッド全体のレイアウト設定
            FlowLayoutPanel layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            // ボタンを追加
            for (int i = 0; i < buttonNames.Count; i++)
            {
                string buttonName = buttonNames[i];
                string dColumnValue = i < dColumns.Count ? dColumns[i] : "N/A"; // D列情報が不足する場合に備える
                string dColumnIndex = dColumnValue.Replace("Button ", ""); // "Button "を除去して番号を取得

                Button button = new Button
                {
                    Text = buttonName,
                    Size = new Size(150, 50), // ボタンのサイズを調整
                    Margin = new Padding(10), // ボタン間の余白
                    BackColor = Color.Black, // 初期状態は消灯
                    ForeColor = Color.White
                };

                // ボタンのバックライトを更新
                UpdateButtonBacklight(i + 1, button);

                // ボタンが押されたとき (press)
                button.MouseDown += (sender, e) =>
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string logMessage = $"[{timestamp}] Device: {DeviceName}, Button: {button.Text} ({dColumnValue}), press";
                    logAction?.Invoke(logMessage);

                    // Telnetでコマンド送信
                    if (isTelnetConnected())
                    {
                        string telnetCommand = $"#DEVICE,{deviceID},{dColumnIndex},3";
                        sendTelnetCommand?.Invoke(telnetCommand);
                        logAction?.Invoke($"[Telnet Command Sent] {telnetCommand}");
                    }
                };

                // ボタンが離されたとき (release)
                button.MouseUp += (sender, e) =>
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string logMessage = $"[{timestamp}] Device: {DeviceName}, Button: {button.Text} ({dColumnValue}), release";
                    logAction?.Invoke(logMessage);

                    // Telnetでコマンド送信
                    if (isTelnetConnected())
                    {
                        string telnetCommand = $"#DEVICE,{deviceID},{dColumnIndex},4";
                        sendTelnetCommand?.Invoke(telnetCommand);
                        logAction?.Invoke($"[Telnet Command Sent] {telnetCommand}");
                    }
                };

                layout.Controls.Add(button);
                buttonControls[i + 1] = button; // ボタンを管理
            }

            this.Controls.Add(layout);
            this.AutoSize = true;
        }

        /// <summary>
        /// ボタンのバックライトを更新
        /// </summary>
        /// <param name="buttonIndex">ボタン番号</param>
        /// <param name="button">対象のボタン</param>
        //private void UpdateButtonBacklight(int buttonIndex, Button button)
        //{
        //    if (!isTelnetConnected()) // Telnet未接続の場合は消灯
        //    {
        //        button.BackColor = Color.Black;
        //        button.Text = $"{button.Text}\n(OFF)";
        //        return;
        //    }

        //    bool? isActive = getButtonState(buttonIndex); // ボタンの状態を取得
        //    if (isActive.HasValue)
        //    {
        //        int brightness = isActive.Value
        //            ? getActiveBrightness(deviceID) // アクティブ状態の照度
        //            : getInactiveBrightness(deviceID); // インアクティブ状態の照度

        //        // 照度に応じて背景色を変更
        //        int colorValue = Math.Min(255, Math.Max(0, (int)(255 * (brightness / 100.0))));
        //        button.BackColor = Color.FromArgb(colorValue, colorValue, colorValue);
        //        button.Text = $"{buttonNames[buttonIndex - 1]}\n({brightness}%)";
        //    }
        //    else
        //    {
        //        button.BackColor = Color.Black; // 状態が不明な場合は消灯
        //        button.Text = $"{buttonNames[buttonIndex - 1]}\n(UNKNOWN)";
        //    }
        //}

        /// <summary>
        /// ボタンのバックライトを更新
        /// </summary>
        /// <param name="buttonIndex">ボタン番号</param>
        /// <param name="button">対象のボタン</param>
        private void UpdateButtonBacklight(int buttonIndex, Button button)
        {
            if (!isTelnetConnected()) // Telnet未接続の場合は消灯（黒）
            {
                button.BackColor = Color.Black;
                button.Text = $"{buttonNames[buttonIndex - 1]}\n(OFF)";
                return;
            }

            bool? isActive = getButtonState(buttonIndex); // ボタンの状態を取得
            if (isActive.HasValue)
            {
                int brightness = isActive.Value
                    ? getActiveBrightness(deviceID) // アクティブ状態の照度
                    : getInactiveBrightness(deviceID); // インアクティブ状態の照度

                if (brightness == 0) // 照度が0の場合は黒
                {
                    button.BackColor = Color.Black;
                    button.Text = $"{buttonNames[buttonIndex - 1]}\n(0%)";
                }
                else
                {
                    // 照度に応じて背景色を変更（グレーから白の範囲で変化）
                    int colorValue = Math.Min(255, Math.Max(0, (int)(255 * (brightness / 100.0))));
                    button.BackColor = Color.FromArgb(colorValue, colorValue, colorValue);
                    button.Text = $"{buttonNames[buttonIndex - 1]}\n({brightness}%)";
                }
            }
            else
            {
                // 状態が不明な場合は消灯（黒）
                button.BackColor = Color.Black;
                button.Text = $"{buttonNames[buttonIndex - 1]}\n(UNKNOWN)";
            }
        }

        /// <summary>
        /// 全ボタンのバックライトを更新
        /// </summary>
        public void UpdateAllButtonsBacklight()
        {
            foreach (var kvp in buttonControls)
            {
                UpdateButtonBacklight(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// ボタンの状態を更新し、デザインに反映
        /// </summary>
        /// <param name="buttonIndex">ボタン番号</param>
        /// <param name="isActive">アクティブ状態</param>
        public void UpdateButtonState(int buttonIndex, bool isActive)
        {
            if (buttonControls.ContainsKey(buttonIndex))
            {
                // ボタン状態をデータとして更新
                buttonControls[buttonIndex].Text = isActive ? "Active" : "Inactive";

                // デザインを更新
                UpdateButtonBacklight(buttonIndex, buttonControls[buttonIndex]);
            }
        }

    }
}
