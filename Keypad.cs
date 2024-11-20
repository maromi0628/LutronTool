using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RoomKeypadManager
{
    public class Keypad : Panel
    {
        private int type; // ボタン数 (2連, 3連, 4連)
        private List<Button> buttons;
        private TelnetClientHelper telnetClientHelper; // Telnetヘルパー
        private const int ButtonHeight = 50;
        private const int ButtonSpacing = 10;

        public Keypad(int type, List<string> buttonNames)
        {
            this.type = type;
            this.telnetClientHelper = new TelnetClientHelper(); // ヘルパーを初期化
            InitializeKeypad(buttonNames);
        }

        private void InitializeKeypad(List<string> buttonNames)
        {
            this.Size = new Size(150, (ButtonHeight + ButtonSpacing) * type);
            this.BackColor = Color.LightGray;
            buttons = new List<Button>();

            for (int i = 0; i < type; i++)
            {
                Button button = new Button
                {
                    Text = i < buttonNames.Count ? buttonNames[i] : "Button " + (i + 1),
                    Height = ButtonHeight,
                    Width = 120,
                    Top = i * (ButtonHeight + ButtonSpacing),
                    Left = 15,
                    BackColor = Color.LightBlue
                };

                // SW1の1番目のボタンのみGETTIMEコマンド送信を設定
                if (i == 0)
                {
                    button.Click += async (sender, e) => await SendGetTimeCommandAsync();
                }
                else
                {
                    button.Click += (sender, e) => ToggleButton((Button)sender);
                }

                buttons.Add(button);
                this.Controls.Add(button);
            }
        }

        private async Task SendGetTimeCommandAsync()
        {
            string ipAddress = "192.168.100.3";
            int port = 23;

            try
            {
                bool isConnected = await telnetClientHelper.ConnectAsync(ipAddress, port);
                if (!isConnected)
                {
                    MessageBox.Show("Telnet接続に失敗しました");
                    return;
                }

                bool isLoggedIn = await telnetClientHelper.LoginAsync("x1s", "x1s");
                if (!isLoggedIn)
                {
                    MessageBox.Show("ログインに失敗しました");
                    return;
                }

                string response = await telnetClientHelper.SendCommandAsync("GETTIME");
                MessageBox.Show($"GETTIME応答: {response}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラー: {ex.Message}");
            }
            finally
            {
                telnetClientHelper.Close();
            }
        }

        private void ToggleButton(Button button)
        {
            if (button.BackColor == Color.LightBlue)
            {
                button.BackColor = Color.DarkBlue;
            }
            else
            {
                button.BackColor = Color.LightBlue;
            }
        }
    }
}

