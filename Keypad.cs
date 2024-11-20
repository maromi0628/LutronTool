using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RoomKeypadManager
{
    public class Keypad : Panel
    {
        private int type;
        private List<Button> buttons;
        private const int ButtonHeight = 50;
        private const int ButtonSpacing = 10;

        public Keypad(int type)
        {
            this.type = type;
            this.InitializeKeypad();
        }

        private void InitializeKeypad()
        {
            this.Size = new Size(150, (ButtonHeight + ButtonSpacing) * type);
            this.BackColor = Color.LightGray;
            buttons = new List<Button>();

            for (int i = 0; i < type; i++)
            {
                Button button = new Button
                {
                    Text = $"Button {i + 1}",
                    Height = ButtonHeight,
                    Width = 120,
                    Top = i * (ButtonHeight + ButtonSpacing),
                    Left = 15,
                    BackColor = Color.LightBlue
                };
                button.Click += (sender, e) => ToggleButton((Button)sender);
                buttons.Add(button);
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
