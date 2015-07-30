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
        private Dictionary<Keys, Action> keyToCommand = new Dictionary<Keys, Action>(); 

        public WindowSelector()
        {
            InitializeComponent();

            Config.Init();
            exclusions = ReadExclusions();

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

            keyToCommand[Keys.Left] = OnLeft;
            keyToCommand[Keys.Up] = OnUp;
            keyToCommand[Keys.Right] = OnRight;
            keyToCommand[Keys.Down] = OnDown;

            keyToCommand[Keys.NumPad1] = On1;
            keyToCommand[Keys.NumPad2] = On2;
            keyToCommand[Keys.NumPad3] = On3;
            keyToCommand[Keys.NumPad4] = On4;
            keyToCommand[Keys.NumPad5] = On5;
            keyToCommand[Keys.NumPad6] = On6;
            keyToCommand[Keys.NumPad7] = On7;
            keyToCommand[Keys.NumPad8] = On8;
            keyToCommand[Keys.NumPad9] = On9;


            mouseListener = new MouseHookListener(new GlobalHooker());
            mouseListener.Enabled = true;
            mouseListener.MouseMoveExt += OnMouseDown;

            keyboardListener = new KeyboardHookListener(new GlobalHooker());
            keyboardListener.Enabled = true;

            keyboardListener.KeyDown += OnKeyDown;
            keyboardListener.KeyUp += OnKeyUp;


            StartWindowUpdater();
        }

        private void On1()
        {
            throw new NotImplementedException();
        }

        private void On2()
        {
            throw new NotImplementedException();
        }

        private void On3()
        {
            throw new NotImplementedException();
        }

        private void On4()
        {
            throw new NotImplementedException();
        }

        private void On5()
        {
            throw new NotImplementedException();
        }

        private void On6()
        {
            throw new NotImplementedException();
        }

        private void On7()
        {
            throw new NotImplementedException();
        }

        private void On8()
        {
            throw new NotImplementedException();
        }

        private void On9()
        {
            throw new NotImplementedException();
        }

        private void OnDown()
        {
            throw new NotImplementedException();
        }

        private void OnRight()
        {
            throw new NotImplementedException();
        }

        private void OnUp()
        {
            throw new NotImplementedException();
        }

        private void OnLeft()
        {
            throw new NotImplementedException();
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
                if (keyToCommand.ContainsKey(e.KeyCode))
                {
                    keyToCommand[e.KeyCode]();
                }
            }
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
