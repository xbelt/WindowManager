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
        private Dictionary<Keys, bool> activationKeys = new Dictionary<Keys, bool>(); 
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
            trayMenu.MenuItems.Add("Settings", OnSettings);

            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
            trayIcon = new NotifyIcon();
            trayIcon.Text = "WindowSelector";
            trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);

            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            activationKeys[Keys.LShiftKey] = false;
            activationKeys[Keys.LMenu] = false;
            
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
                keyToConfigList[keyValue.Key] = JsonConvert.DeserializeObject<List<Position>>(Config.Settings["Positions"][keyValue.Value].Value);
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (activationKeys.ContainsKey(e.KeyCode))
            {
                activationKeys[e.KeyCode] = false;
            }
        }


        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine("OnKeyDown");
            if (activationKeys.ContainsKey(e.KeyCode))
            {
                activationKeys[e.KeyCode] = true;
            }

            Console.WriteLine(activationKeys.Values.All(x => x));
            Console.WriteLine(activationKeys.Aggregate(e.KeyCode.ToString(), (s, pair) => s + "(" + pair.Key + ")" + pair.Value));

            if (activationKeys.Values.All(x => x))
            {
                Console.WriteLine("AllActive");
                if (keyToSerializedName.ContainsKey(e.KeyCode))
                {
                    PerformClick(e.KeyCode);
                }
            }
        }

        private void PerformClick(Keys keyCode)
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
                    //Console.WriteLine(CurrentWindows.Aggregate("", (s, pair) => s + ", " + pair.Value));
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
            throw new NotImplementedException();
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
