using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DdcTraySwitcher
{
    public class TrayApp : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private int selectedMonitor;
        private uint selectedInput;

        private const string RegistryBasePath = @"Software\\DdcTraySwitcher";

        public TrayApp()
        {
            LoadSettings();
            MonitorHelper.Initialize();

            trayIcon = new NotifyIcon()
            {
                Icon = new Icon(typeof(TrayApp).Assembly.GetManifestResourceStream("DdcTraySwitcher.Resources.icon.ico")),
                Visible = true,
                Text = "DDC Input Switcher",
                ContextMenuStrip = BuildMenu()
            };

            trayIcon.MouseClick += OnClick;
        }

        private void LoadSettings()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryBasePath);
            selectedMonitor = Convert.ToInt32(key?.GetValue("SelectedMonitor") ?? 0);
            selectedInput = Convert.ToUInt32(key?.GetValue("SelectedInput") ?? 0x11);
        }

        private void SaveSettings()
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryBasePath);
            key?.SetValue("SelectedMonitor", selectedMonitor, RegistryValueKind.DWord);
            key?.SetValue("SelectedInput", selectedInput, RegistryValueKind.DWord);
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();

            var monitorMenu = new ToolStripMenuItem("Choose monitor");
            for (int i = 0; i < MonitorHelper.Monitors.Count; i++)
            {
                int idx = i;
                string name = MonitorHelper.Monitors[i].szPhysicalMonitorDescription;
                monitorMenu.DropDownItems.Add(new ToolStripMenuItem(name, null, (s, e) =>
                {
                    selectedMonitor = idx;
                    SaveSettings();
                }));
            }

            var inputMenu = new ToolStripMenuItem("Choose input source");
            inputMenu.DropDownItems.Add(new ToolStripMenuItem("HDMI 1", null, (s, e) => { selectedInput = 0x11; SaveSettings(); }));
            inputMenu.DropDownItems.Add(new ToolStripMenuItem("HDMI 2", null, (s, e) => { selectedInput = 0x12; SaveSettings(); }));
            inputMenu.DropDownItems.Add(new ToolStripMenuItem("DP 1", null,   (s, e) => { selectedInput = 0x0F; SaveSettings(); }));
            inputMenu.DropDownItems.Add(new ToolStripMenuItem("VGA", null,    (s, e) => { selectedInput = 0x01; SaveSettings(); }));

            menu.Items.Add(monitorMenu);
            menu.Items.Add(inputMenu);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Deinstall", null, (s, e) => Uninstall()));
            menu.Items.Add(new ToolStripMenuItem("Quit", null, (s, e) => Exit()));

            return menu;
        }

        private void Uninstall()
        {
            Registry.CurrentUser.DeleteSubKeyTree(RegistryBasePath, false);

            using var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            runKey?.DeleteValue("DdcTraySwitcher", false);

            MessageBox.Show("Settings and autostart removed.", "Deinstall", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Clicks == 2)
            {
                MonitorHelper.SetInput(selectedMonitor, selectedInput);
            }
        }

        private void Exit()
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            MonitorHelper.Dispose();
            Application.Exit();
        }
    }
}