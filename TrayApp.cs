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

            trayIcon.MouseDoubleClick += OnDoubleClick;
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
        
        private void RebuildMenu()
        {
            trayIcon.ContextMenuStrip = null;
            trayIcon.ContextMenuStrip = BuildMenu();
        }
        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();

            var monitorMenu = new ToolStripMenuItem("Choose monitor");

            for (int i = 0; i < MonitorHelper.Monitors.Count; i++)
            {
                int idx = i;
                string raw = EdidReader.GetMonitorName(i);
                string name = $"{raw} (#{i + 1})";

                var item = new ToolStripMenuItem(name)
                {
                    Checked = (selectedMonitor == idx)
                };

                item.Click += (s, e) =>
                {
                    selectedMonitor = idx;
                    SaveSettings();
                    RebuildMenu();
                };

                monitorMenu.DropDownItems.Add(item);
            }

            var inputMenu = new ToolStripMenuItem("Choose input");

            void AddInput(string label, uint value)
            {
                var item = new ToolStripMenuItem(label)
                {
                    Checked = (selectedInput == value)
                };

                item.Click += (s, e) =>
                {
                    selectedInput = value;
                    SaveSettings();
                    RebuildMenu();
                };

                inputMenu.DropDownItems.Add(item);
            }

            AddInput("HDMI 1", 0x11);
            AddInput("HDMI 2", 0x12);
            AddInput("DP 1", 0x0F);
            AddInput("VGA", 0x01);

            var autostartItem = new ToolStripMenuItem("Activate autostart", null, ToggleAutostart)
            {
                Checked = AutoStart.IsRegistered()
            };

            menu.Items.Add(monitorMenu);
            menu.Items.Add(inputMenu);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(autostartItem);
            menu.Items.Add(new ToolStripMenuItem("Quit", null, (s, e) => Exit()));

            return menu;
        }


        private void ToggleAutostart(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item)
            {
                if (item.Checked)
                {
                    AutoStart.Unregister();
                    item.Checked = false;
                }
                else
                {
                    AutoStart.Register();
                    item.Checked = true;
                }
            }
        }

        private void OnClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Clicks == 2)
            {
                bool result = MonitorHelper.SetInput(selectedMonitor, selectedInput);
                if (!result)
                {
                    MessageBox.Show($"Input could not be set!\nMonitor: {selectedMonitor}, Input: 0x{selectedInput:X}", 
                        "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                bool result = MonitorHelper.SetInput(selectedMonitor, selectedInput);
                if (!result)
                {
                    MessageBox.Show($"Input could not be set!\nMonitor: {selectedMonitor}, Input: 0x{selectedInput:X}", 
                        "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
