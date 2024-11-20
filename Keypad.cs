using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RoomKeypadManager
{
    public class Keypad : Panel
    {
        private const int ButtonHeight = 50;
        private const int ButtonSpacing = 10;

        public Keypad(int buttonCount, List<string> buttonNames)
        {
            this.Size = new Size(150, (ButtonHeight + ButtonSpacing) * buttonCount);
            this.BackColor = Color.LightGray;

            for (int i = 0; i < buttonCount; i++)
            {
                Button button = new Button
                {
                    Text = i < buttonNames.Count ? buttonNames[i] : $"Button {i + 1}",
                    Height = ButtonHeight,
                    Width = 120,
                    Top = i * (ButtonHeight + ButtonSpacing),
                    Left = 15,
                    BackColor = Color.LightBlue
                };
                button.Click += (sender, e) => ToggleButton((Button)sender);
                this.Controls.Add(button);
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
