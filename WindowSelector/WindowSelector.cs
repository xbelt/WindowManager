using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using Newtonsoft.Json;
using Utility.ModifyRegistry;
using WindowSelector.Configuration;

namespace WindowSelector
{
    public partial class WindowSelector : Form
    {
        private readonly List<string> _exclusions;

        private Screen _currentScreen = Screen.PrimaryScreen;
        private readonly Dictionary<Keys, List<Position>> _keyToConfigList = new Dictionary<Keys, List<Position>>();
        private readonly Dictionary<Keys, string> _keyToSerializedName = new Dictionary<Keys, string>
        {
            {Keys.NumPad1, "N1" },
            {Keys.NumPad2, "N2" },
            {Keys.NumPad3, "N3" },
            {Keys.NumPad4, "N4" },
            {Keys.NumPad5, "N5" },
            {Keys.NumPad6, "N6" },
            {Keys.NumPad7, "N7" },
            {Keys.NumPad8, "N8" },
            {Keys.NumPad9, "N9" },
        }; 

        private readonly Dictionary<Keys, bool> _hasBeenPressend = new Dictionary<Keys, bool>(); 
        private Keys _currentKey = Keys.None;
        private int _currentIndex = 0;
        private int _windowsVersion = 0;

        private readonly List<string> _processException = new List<string>
        {
            "devenv.exe",
            "ONENOTE.EXE",
            "WINWORD.EXE",
            "EXCEL.EXE"
        };

        private int WindowsVersion
        {
            get
            {
                if (_windowsVersion != 0) return _windowsVersion;
                var temp = new ModifyRegistry();
                var read = temp.Read("CurrentMajorVersionNumber");
                if (read != null)
                    _windowsVersion = (int) read;
                return _windowsVersion;
            }
        }

        private int WindowOffset()
        {
            return WindowsVersion >= 10 ? 4 : 0;
        }

        private int WindowOffset(string processName)
        {
            if (WindowOffset() != 4) return WindowOffset();
            return _processException.Any(processName.EndsWith) ? 0 : WindowOffset();
        }

        public WindowSelector()
        {
            InitializeComponent();
            Size = new Size(0,0);
            
            Config.Init();
            _exclusions = ReadExclusions();

            _hasBeenPressend[Keys.Add] = false;


            //var json = JsonConvert.SerializeObject(testList, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings {StringEscapeHandling = StringEscapeHandling.EscapeHtml});
            //Console.WriteLine(json);

            InitializePositions();

            var trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Layout settings", OnSettings);
            trayMenu.MenuItems.Add("Exit", OnExit);

            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
            var trayIcon = new NotifyIcon
            {
                Text = "WindowSelector",
                Icon = new Icon(SystemIcons.Application, 40, 40),
                ContextMenu = trayMenu,
                Visible = true
            };

            var mouseListener = new MouseHookListener(new GlobalHooker()) {Enabled = true};
            mouseListener.MouseMoveExt += OnMouseDown;

            var keyboardListener = new KeyboardHookListener(new GlobalHooker()) {Enabled = true};

            keyboardListener.KeyDown += OnKeyDown;
            keyboardListener.KeyUp += OnKeyUp;

            Opacity = 0.15f;
            Size = new Size(0, 0);
        }

        private void InitializePositions()
        {
            foreach (var keyValue in _keyToSerializedName)
            {
                var keyToConfig = JsonConvert.DeserializeObject<List<Position>>(Config.Settings["Positions"][keyValue.Value].Value);
                _keyToConfigList[keyValue.Key] = keyToConfig;
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
            //ActivateWindows();
            //Size = new Size(0,0);
        }

        private void ActivateWindows()
        {
            var possibleWindows = new List<IntPtr>();
            LoadOpenWindows();
            foreach (var currentWindow in CurrentWindows)
            {
                var size = Helper.GetWindowDimensions(currentWindow.Key);

                if (Location.X >= size.Left + WindowOffset())
                    continue;
                if (Location.Y >= size.Top)
                    continue;
                if (Location.X + Size.Width <= size.Right - WindowOffset())
                    continue;
                if (Location.Y + Size.Height <= size.Bottom - WindowOffset())
                    continue;

                possibleWindows.Add(currentWindow.Key);
            }
            if (!possibleWindows.Any())
            {
                return;
            }
            if (possibleWindows.Count == 1)
            {
                Helper.SetForegroundWindow(possibleWindows.First());
            }
            else
            {
                var minValue = possibleWindows.Min(x => Helper.GetZOrder(x));
                Helper.SetForegroundWindow(possibleWindows.Single(x => Helper.GetZOrder(x) == minValue));
            }
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

        private void MoveApplication(Keys keyCode)
        {
            if (_currentKey == keyCode)
            {
                _currentIndex = (_currentIndex + 1) % _keyToConfigList[keyCode].Count;
            }
            else
            {
                _currentIndex = 0;
                _currentKey = keyCode;
            }

            var currentPos = _keyToConfigList[keyCode][_currentIndex];

            var activeWindow = Helper.GetForegroundWindow();
            uint lpdwProcessId;
            Helper.GetWindowThreadProcessId(activeWindow, out lpdwProcessId);

            var hProcess = Helper.OpenProcess(0x0410, false, lpdwProcessId);

            var text = new StringBuilder(1000);
            //GetModuleBaseName(hProcess, IntPtr.Zero, text, text.Capacity);
            Helper.GetModuleFileNameEx(hProcess, IntPtr.Zero, text, text.Capacity);

            var processName = text.ToString();


            var location = new Point(_currentScreen.WorkingArea.Left + (int)(_currentScreen.WorkingArea.Width * currentPos.X / 100) - WindowOffset(processName),
                _currentScreen.WorkingArea.Top + (int)(_currentScreen.WorkingArea.Height * currentPos.Y / 100));
            var size = new Size((int)(_currentScreen.WorkingArea.Width * currentPos.Width / 100) + 2 * WindowOffset(processName),
                (int)(_currentScreen.WorkingArea.Height * currentPos.Height / 100) + WindowOffset(processName));
            

            Helper.SetWindowPos(activeWindow, 0, location.X, location.Y, size.Width, size.Height,
                0x0004 | 0x0040);


        }

        private void MoveForm(Keys keyCode)
        {
            Helper.SetForegroundWindow(Handle.ToInt32());

            if (_currentKey == keyCode)
            {
                _currentIndex = (_currentIndex + 1)%_keyToConfigList[keyCode].Count;
            }
            else
            {
                _currentIndex = 0;
                _currentKey = keyCode;
            }

            var currentPos = _keyToConfigList[keyCode][_currentIndex];
            Location = new Point(_currentScreen.WorkingArea.Left + (int)(_currentScreen.WorkingArea.Width * currentPos.X / 100),
                _currentScreen.WorkingArea.Top + (int)(_currentScreen.WorkingArea.Height * currentPos.Y / 100));
            Size = new Size((int)(_currentScreen.WorkingArea.Width * currentPos.Width / 100),
                (int)(_currentScreen.WorkingArea.Height * currentPos.Height / 100));
        }

        private void OnMouseDown(object sender, MouseEventExtArgs e)
        {
            var newScreen = Screen.FromPoint(Cursor.Position);
            if (newScreen.Equals(_currentScreen)) return;
            _currentScreen = newScreen;
            Location = new Point(_currentScreen.WorkingArea.Left, _currentScreen.WorkingArea.Top);
            Size = new Size(0, 0);
        }

        private void LoadOpenWindows()
        {
            var windows = OpenWindowGetter.GetOpenWindows();
            var placement = new Helper.WINDOWPLACEMENT();

            CurrentWindows.Clear();

            foreach (var window in windows)
            {
                Helper.GetWindowPlacement(window.Key, ref placement);
                if (placement.showCmd != 2 &&
                    window.Value != Application.ProductName &&
                    !_exclusions.Any(x => window.Value.Contains(x)))
                {
                    CurrentWindows.Add(window);
                }
                    
            }
        }

        private List<KeyValuePair<IntPtr, string>> CurrentWindows { get; set; } = new List<KeyValuePair<IntPtr, string>>();

        private void OnSettings(object sender, EventArgs e)
        {
            var window = new LayoutSettings();
            window.Show();
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private static List<string> ReadExclusions()
        {
            return Config.Settings["Exclude"].Children().Select(childrenName => childrenName.Name).ToList();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Opacity = 0.15f;
        }
    }
}
