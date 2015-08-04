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

        private const int Offset = 7;

        private int WindowOffset()
        {
            return WindowsVersion >= 10 ? Offset : 0;
        }

        private int WindowOffset(string processName)
        {
            if (WindowOffset() != Offset) return WindowOffset();
            return _processExclusions.Any(processName.EndsWith) ? 0 : WindowOffset();
        }

        public WindowSelector()
        {
            InitializeComponent();
            ExceptionlessClient.Default.Register();
            Config.Init();
            if (!HasValidLicense())
            {
                Application.Exit();
            }

            Size = new Size(0,0);
            
            _exclusions = ReadExclusions();
            _processExclusions = ReadProcessExclusions();

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

            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
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

        private bool HasValidLicense()
        {
            string correctKey = null;
            var actualKey = "";
        
            try
            {
                correctKey = CalculateMD5Hash(Config.Settings["Settings"]["Email"].Value +
                                                  "9o7X^jdqAMdG7oOMQYnZ&WgJ5@xexEtlFrCWQnHEfm*5E13%@ce3#WElk*uPRGd^dHd4@xDKFNK#VBkSZ7o%7nCQDRMJONY799t@")
                    .Substring(0, 10);
                actualKey = Config.Settings["Settings"]["License"].Value;
            }
            catch (Exception e)
            {
                e.ToExceptionless().MarkAsCritical().AddTags("reading activation").Submit();
            }

            while (correctKey != actualKey)
            {
                if (
                    MessageBox.Show("Please enter correct license information", "Mîssing License",
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Error) == DialogResult.OK)
                {
                    var dialog = new Activation();
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        actualKey = Config.Settings["Settings"]["License"].Value;
                        correctKey = CalculateMD5Hash(Config.Settings["Settings"]["Email"].Value +
                                                  "9o7X^jdqAMdG7oOMQYnZ&WgJ5@xexEtlFrCWQnHEfm*5E13%@ce3#WElk*uPRGd^dHd4@xDKFNK#VBkSZ7o%7nCQDRMJONY799t@")
                    .Substring(0, 10);
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;

        }

        public string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private List<string> ReadProcessExclusions()
        {
            try
            {
                var result = new List<string>();
                var connectionString = new MySqlConnectionStringBuilder();
                connectionString.UserID = Config.Settings["MySql"]["Username"].Value;
                connectionString.Password = Config.Settings["MySql"]["Password"].Value;
                connectionString.Database = Config.Settings["MySql"]["Database"].Value;
                connectionString.Port = (uint) Config.Settings["MySql"]["Port"].intValue;
                connectionString.Server = Config.Settings["MySql"]["Host"].Value;
                using (var connection = new MySqlConnection(connectionString.ConnectionString))
                {
                    connection.Open();
                    using (var reader = new MySqlCommand("SELECT process FROM exclusions", connection).ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(reader.GetString("process"));
                        }
                    }

                }
                return result;
            }
            catch (Exception e)
            {
                e.ToExceptionless().AddTags("mysql", "table:exclusions").Submit();
                return new List<string>();
            }
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

                if (Location.X > size.Left + WindowOffset())
                    continue;
                if (Location.Y > size.Top)
                    continue;
                if (Location.X + Size.Width < size.Right - WindowOffset())
                    continue;
                if (Location.Y + Size.Height < size.Bottom - WindowOffset())
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

        private static List<string> ReadExclusions()
        {
            try
            {
                var result = new List<string>();
                var connectionString = new MySqlConnectionStringBuilder();
                connectionString.UserID = Config.Settings["MySql"]["Username"].Value;
                connectionString.Password = Config.Settings["MySql"]["Password"].Value;
                connectionString.Database = Config.Settings["MySql"]["Database"].Value;
                connectionString.Port = (uint) Config.Settings["MySql"]["Port"].intValue;
                connectionString.Server = Config.Settings["MySql"]["Host"].Value;
                using (var connection = new MySqlConnection(connectionString.ConnectionString))
                {
                    connection.Open();
                    using (var reader = new MySqlCommand("SELECT windows FROM windows", connection).ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(reader.GetString("windows"));
                        }
                    }

                }
                return result;
            }
            catch (Exception e)
            {
                e.ToExceptionless().MarkAsCritical().AddTags("mysql", "table:windows").Submit();
                return new List<string>();
            }
        }
    }
}
