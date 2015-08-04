using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MouseKeyboardActivityMonitor;
using WindowSelector.Configuration;

namespace WindowSelector
{
    partial class WindowSelector
    {
        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Opacity = 0.15f;
        }

        private void OnSettings(object sender, EventArgs e)
        {
            var window = new LayoutSettings();
            window.Show();
            window.FormClosed += (o, args) =>
            {
                BackColor = Color.FromArgb(Config.Settings["Settings"]["Color"].intValue);
                InitializePositions();
            };
        }

        private void OnMouseDown(object sender, MouseEventExtArgs e)
        {
            var newScreen = Screen.FromPoint(Cursor.Position);
            if (newScreen.Equals(_currentScreen)) return;
            _currentScreen = newScreen;
            Location = new Point(_currentScreen.WorkingArea.Left, _currentScreen.WorkingArea.Top);
            Size = new Size(0, 0);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_hasBeenPressend.ContainsKey(e.KeyCode) && _hasBeenPressend[e.KeyCode])
                return;
            _hasBeenPressend[e.KeyCode] = true;

            if (_keyToSerializedName.ContainsKey(e.KeyCode) && e.Alt && e.Control && _hasBeenPressend[Keys.Add])
            {
                MoveForm(e.KeyCode);
                return;
            }

            if (_keyToSerializedName.ContainsKey(e.KeyCode) && e.Alt && e.Control)
            {
                MoveApplication(e.KeyCode);
            }

        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            _hasBeenPressend[e.KeyCode] = false;

            if ((_hasBeenPressend.ContainsKey(Keys.Add) && !_hasBeenPressend[Keys.Add]) &&
                (_hasBeenPressend.ContainsKey(Keys.LControlKey) && !_hasBeenPressend[Keys.LControlKey]) &&
                (_hasBeenPressend.ContainsKey(Keys.LMenu) && !_hasBeenPressend[Keys.LMenu]))
            {
                _currentKey = Keys.None;
                _currentIndex = 0;
                ActivateWindows();
                Size = new Size(0, 0);
            }
        }
    }
}
