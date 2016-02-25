using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Exceptionless;
using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using MySql.Data.MySqlClient;
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

        private readonly List<string> _processExclusions;

        private MouseHookListener mouseListener;
        private KeyboardHookListener keyboardListener;

        public WindowSelector()
        {
            InitializeComponent();
            ExceptionlessClient.Default.Register();
            Config.Init();

            Size = new Size(0,0);

            _hasBeenPressend[Keys.Add] = false;

            try
            {
                BackColor = Color.FromArgb(Config.Settings["Settings"]["Color"].intValue);
            }
            catch (Exception e)
            {
                e.ToExceptionless().AddTags("changeColor").Submit();
            }

            InitializePositions();

            var trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Settings", OnSettings);
            trayMenu.MenuItems.Add("Exit", OnExit);
            
            var trayIcon = new NotifyIcon
            {
                Text = "Window Selector",
                Icon = new Icon(GetType(), "Icon.ico"),
                ContextMenu = trayMenu,
                Visible = true
            };

            mouseListener = new MouseHookListener(new GlobalHooker());
            mouseListener.Enabled = true;
            mouseListener.MouseMoveExt += OnMouseDown;

            keyboardListener = new KeyboardHookListener(new GlobalHooker());
            keyboardListener.Enabled = true;

            keyboardListener.KeyDown += OnKeyDown;
            keyboardListener.KeyUp += OnKeyUp;

            Opacity = 0.15f;
            Size = new Size(0, 0);
            WindowState = FormWindowState.Normal;
        }

        private void InitializePositions()
        {
            try
            {
                foreach (var keyValue in _keyToSerializedName)
                {
                    var keyToConfig =
                        JsonConvert.DeserializeObject<List<Position>>(Config.Settings["Positions"][keyValue.Value].Value);
                    _keyToConfigList[keyValue.Key] = keyToConfig;
                }
            }
            catch (Exception e)
            {
                e.ToExceptionless().AddTags("deserialize").MarkAsCritical().Submit();
            }
        }

        private void ActivateWindows()
        {
            var possibleWindows = new List<IntPtr>();
            LoadOpenWindows();
            foreach (var currentWindow in CurrentWindows)
            {
                var size = Helper.GetWindowDimensions(currentWindow.Key);

                if (Location.X > size.Left)
                    continue;
                if (Location.Y > size.Top)
                    continue;
                if (Location.X + Size.Width < size.Right)
                    continue;
                if (Location.Y + Size.Height < size.Bottom)
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

            Position currentPos = null;
            try
            {
                currentPos = _keyToConfigList[keyCode][_currentIndex];
            }
            catch (Exception e)
            {
                e.ToExceptionless().AddTags("_keyToConfigList").Submit();
                return;
            }

            var activeWindow = Helper.GetForegroundWindow();
            uint lpdwProcessId;
            Helper.GetWindowThreadProcessId(activeWindow, out lpdwProcessId);

            var hProcess = Helper.OpenProcess(0x0410, false, lpdwProcessId);

            var text = new StringBuilder(1000);
            Helper.GetModuleFileNameEx(hProcess, IntPtr.Zero, text, text.Capacity);

            var processName = text.ToString();

            var location = new Point(_currentScreen.WorkingArea.Left + (int)(_currentScreen.WorkingArea.Width * currentPos.X / 100),
                _currentScreen.WorkingArea.Top + (int)(_currentScreen.WorkingArea.Height * currentPos.Y / 100));
            var size = new Size((int)(_currentScreen.WorkingArea.Width * currentPos.Width / 100),
                (int)(_currentScreen.WorkingArea.Height * currentPos.Height / 100));
            

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

        private void LoadOpenWindows()
        {
            var windows = OpenWindowGetter.GetOpenWindows();
            var placement = new Helper.WINDOWPLACEMENT();

            CurrentWindows.Clear();

            foreach (var window in windows)
            {
                if (window.Value == null)
                {
                    continue;
                }
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
    }
}
