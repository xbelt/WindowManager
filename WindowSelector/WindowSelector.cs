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
            {Keys.End, "N1" },
            {Keys.Down, "N2" },
            {Keys.Next, "N3" },
            {Keys.Left, "N4" },
            {Keys.Clear, "N5" },
            {Keys.Right, "N6" },
            {Keys.Home, "N7" },
            {Keys.Up, "N8" },
            {Keys.PageUp, "N9" },
        }; 

        private Dictionary<Keys, bool> hasBeenPressend = new Dictionary<Keys, bool>(); 
        private Keys currentKey = Keys.None;
        private int currentIndex = 0;

        public WindowSelector()
        {
            InitializeComponent();

            Config.Init();
            exclusions = ReadExclusions();


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
            Console.WriteLine("OnKeyUp" + e.KeyCode);
           
                //ActivateWindows();
                //Size = new Size(0,0);
        }

        private void ActivateWindows()
        {
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

                Helper.SetForegroundWindow(currentWindow.Key);
            }
        }


        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (hasBeenPressend.ContainsKey(e.KeyCode) && hasBeenPressend[e.KeyCode])
                return;
            Console.WriteLine(e.KeyCode);
            Console.WriteLine(e.Modifiers);
            hasBeenPressend[e.KeyCode] = true;
            //Console.WriteLine("KeyDown: " + e.KeyCode);
            

            Console.WriteLine(keyToSerializedName.ContainsKey(e.KeyCode));
            if (keyToSerializedName.ContainsKey(e.KeyCode) && e.Alt && e.Control)
            {
                MoveForm(e.KeyCode);
            }

        }

        private void MoveForm(Keys keyCode)
        {
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
            Location = new Point((int)(currentScreen.Bounds.Width * currentPos.X / 100),
                (int)(currentScreen.Bounds.Height * currentPos.Y / 100));
            Size = new Size((int)(currentScreen.Bounds.Width * currentPos.Width / 100),
                (int)(currentScreen.Bounds.Height * currentPos.Height / 100));
        }

        private void OnMouseDown(object sender, MouseEventExtArgs e)
        {
            var newScreen = Screen.FromPoint(Cursor.Position);
            if (newScreen != currentScreen)
            {
                currentScreen = newScreen;
                Location = new Point(currentScreen.WorkingArea.Left, currentScreen.WorkingArea.Top);
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
                    !window.Value.Contains(Application.ProductName) &&
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
