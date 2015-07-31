using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using Newtonsoft.Json;
using Utilities;
using WindowScrape.Types;
using Config = WindowSelector.Configuration.Config;

namespace WindowSelector
{
    public partial class WindowSelector : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        private List<string> exclusions;
        private MouseHookListener mouseListener;
        private readonly KeyboardHookListener keyboardListener;
        private Screen currentScreen = Screen.PrimaryScreen;
        private List<Keys> activationKeys = new List<Keys>(); 
        private Dictionary<Keys, List<Position>> keyToConfigList = new Dictionary<Keys, List<Position>>();
        private Dictionary<Keys, string> keyToSerializedName = new Dictionary<Keys, string>
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

        private Dictionary<Keys, bool> hasBeenPressend = new Dictionary<Keys, bool>(); 
        private Keys currentKey = Keys.None;
        private int currentIndex = 0;

        public WindowSelector()
        {
            InitializeComponent();
            Size = new Size(0,0);

            Config.Init();
            exclusions = ReadExclusions();

            hasBeenPressend[Keys.Add] = false;


            //var json = JsonConvert.SerializeObject(testList, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings {StringEscapeHandling = StringEscapeHandling.EscapeHtml});
            //Console.WriteLine(json);

            InitializePositions();

            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Exit", OnExit);
            trayMenu.MenuItems.Add("Layout settings", OnSettings);

            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
            trayIcon = new NotifyIcon();
            trayIcon.Text = "WindowSelector";
            trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);

            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            activationKeys.Add(Keys.Shift);
            activationKeys.Add(Keys.Alt);
            
            mouseListener = new MouseHookListener(new GlobalHooker());
            mouseListener.Enabled = true;
            mouseListener.MouseMoveExt += OnMouseDown;

            keyboardListener = new KeyboardHookListener(new GlobalHooker());
            keyboardListener.Enabled = true;

            keyboardListener.KeyDown += OnKeyDown;
            keyboardListener.KeyUp += OnKeyUp;

            Opacity = 0.15f;
            Size = new Size(0, 0);


            StartWindowUpdater();
        }

        private void InitializePositions()
        {
            foreach (var keyValue in keyToSerializedName)
            {
                var keyToConfig = JsonConvert.DeserializeObject<List<Position>>(Config.Settings["Positions"][keyValue.Value].Value);
                keyToConfigList[keyValue.Key] = keyToConfig;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            hasBeenPressend[e.KeyCode] = false;
            
            if ((hasBeenPressend.ContainsKey(Keys.Add) && !hasBeenPressend[Keys.Add]) && 
                (hasBeenPressend.ContainsKey(Keys.LControlKey) && !hasBeenPressend[Keys.LControlKey]) &&
                (hasBeenPressend.ContainsKey(Keys.LMenu) && !hasBeenPressend[Keys.LMenu]))
            {
                currentKey = Keys.None;
                currentIndex = 0;
                ActivateWindows();
                Size = new Size(0, 0);
            }
            //ActivateWindows();
            //Size = new Size(0,0);
        }

        private void ActivateWindows()
        {
            var possibleWindows = new List<IntPtr>();
            foreach (var currentWindow in CurrentWindows)
            {
                var size = Helper.GetWindowDimensions(currentWindow.Key);

                if (Location.X > size.Left + 7)
                    continue;
                if (Location.Y > size.Top)
                    continue;
                if (Location.X + Size.Width < size.Right - 7)
                    continue;
                if (Location.Y + Size.Height < size.Bottom - 7)
                    continue;

                possibleWindows.Add(currentWindow.Key);
            }
            if (!possibleWindows.Any())
            {
                return;
            }
            else if (possibleWindows.Count == 1)
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
            if (hasBeenPressend.ContainsKey(e.KeyCode) && hasBeenPressend[e.KeyCode])
                return;
            hasBeenPressend[e.KeyCode] = true;

            if (keyToSerializedName.ContainsKey(e.KeyCode) && e.Alt && e.Control && hasBeenPressend[Keys.Add])
            {
                MoveForm(e.KeyCode);
                return;
            }

            if (keyToSerializedName.ContainsKey(e.KeyCode) && e.Alt && e.Control)
            {
                MoveApplication(e.KeyCode);
            }

        }

        private void MoveApplication(Keys keyCode)
        {
            if (currentKey == keyCode)
            {
                currentIndex = (currentIndex + 1) % keyToConfigList[keyCode].Count;
            }
            else
            {
                currentIndex = 0;
                currentKey = keyCode;
            }

            var currentPos = keyToConfigList[keyCode][currentIndex];
            var location = new Point(currentScreen.WorkingArea.Left + (int)(currentScreen.WorkingArea.Width * currentPos.X / 100) -4,
                currentScreen.WorkingArea.Top + (int)(currentScreen.WorkingArea.Height * currentPos.Y / 100));
            var size = new Size((int)(currentScreen.WorkingArea.Width * currentPos.Width / 100) + 8,
                (int)(currentScreen.WorkingArea.Height * currentPos.Height / 100) + 4);
            var activeWindow = Helper.GetForegroundWindow();

            Helper.SetWindowPos(activeWindow, 0, location.X, location.Y, size.Width, size.Height,
                0x0004 | 0x0040);


        }

        private void MoveForm(Keys keyCode)
        {
            Helper.SetForegroundWindow(Handle.ToInt32());

            if (currentKey == keyCode)
            {
                currentIndex = (currentIndex + 1)%keyToConfigList[keyCode].Count;
            }
            else
            {
                currentIndex = 0;
                currentKey = keyCode;
            }

            var currentPos = keyToConfigList[keyCode][currentIndex];
            Location = new Point(currentScreen.WorkingArea.Left + (int)(currentScreen.WorkingArea.Width * currentPos.X / 100),
                currentScreen.WorkingArea.Top + (int)(currentScreen.WorkingArea.Height * currentPos.Y / 100));
            Size = new Size((int)(currentScreen.WorkingArea.Width * currentPos.Width / 100),
                (int)(currentScreen.WorkingArea.Height * currentPos.Height / 100));
        }

        private void OnMouseDown(object sender, MouseEventExtArgs e)
        {
            var newScreen = Screen.FromPoint(Cursor.Position);
            if (newScreen != currentScreen)
            {
                currentScreen = newScreen;
                Location = new Point(currentScreen.WorkingArea.Left, currentScreen.WorkingArea.Top);
                Size = new Size(0, 0);
            }

        }

        private void StartWindowUpdater()
        {
            (new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(Config.Settings["Settings"]["UpdateInterval"].intValue * 1000);
                    LoadOpenWindows();
                    //Console.WriteLine(CurrentWindows.Aggregate("", (s, pair) => s + ", " + pair.Value + Helper.GetWindowDimensions(pair.Key).Left));
                }
            }) {IsBackground = true}).Start();
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
                    !exclusions.Any(x => window.Value.Contains(x)))
                {
                    CurrentWindows.Add(window);
                }
                    
            }
        }

        public List<KeyValuePair<IntPtr, string>> CurrentWindows { get; set; } = new List<KeyValuePair<IntPtr, string>>();

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
            var exclusions = new List<string>();
            foreach (var childrenName in Config.Settings["Exclude"].Children())
            {
                exclusions.Add(childrenName.Name);
            }
            return exclusions;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Opacity = 0.15f;
        }
    }
}
