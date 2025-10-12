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
            
            // Validate selected monitor is still valid
            if (selectedMonitor >= MonitorHelper.Monitors.Count)
            {
                selectedMonitor = 0;
                SaveSettings();
            }

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

            var monitorMenu = new ToolStripMenuItem("Select Monitor");

            if (MonitorHelper.Monitors.Count == 0)
            {
                var noMonitorItem = new ToolStripMenuItem("No DDC/CI monitors detected")
                {
                    Enabled = false
                };
                monitorMenu.DropDownItems.Add(noMonitorItem);
            }
            else
            {
                for (int i = 0; i < MonitorHelper.Monitors.Count; i++)
                {
                    int idx = i;
                    var monitor = MonitorHelper.Monitors[i];
                    string monitorName = EdidReader.GetMonitorName(monitor.OriginalMonitorIndex);
                    string name = $"{monitorName} (Monitor #{i + 1})";

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
            }

            var inputMenu = new ToolStripMenuItem("Select Input Source");

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

            // VGA/Analog
            AddInput("VGA / D-Sub", 0x01);
            
            inputMenu.DropDownItems.Add(new ToolStripSeparator());
            
            // DVI
            AddInput("DVI-1", 0x03);
            AddInput("DVI-2", 0x04);
            
            inputMenu.DropDownItems.Add(new ToolStripSeparator());
            
            // HDMI
            AddInput("HDMI-1", 0x11);
            AddInput("HDMI-2", 0x12);
            AddInput("HDMI-3", 0x13);
            
            inputMenu.DropDownItems.Add(new ToolStripSeparator());
            
            // DisplayPort
            AddInput("DisplayPort-1", 0x0F);
            AddInput("DisplayPort-2", 0x10);
            
            inputMenu.DropDownItems.Add(new ToolStripSeparator());
            
            // USB-C / Thunderbolt
            AddInput("USB-C / Thunderbolt", 0x1B);

            bool isAutostartEnabled = AutoStart.IsRegistered();
            var autostartItem = new ToolStripMenuItem(isAutostartEnabled ? "Disable Autostart" : "Enable Autostart", null, ToggleAutostart)
            {
                Checked = isAutostartEnabled
            };

            var switchItem = new ToolStripMenuItem("Switch Input Now", null, (s, e) => ApplySelectedInput())
            {
                Enabled = MonitorHelper.Monitors.Count > 0
            };
            switchItem.Font = new Font(switchItem.Font, FontStyle.Bold);

            menu.Items.Add(monitorMenu);
            menu.Items.Add(inputMenu);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(switchItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(autostartItem);
            menu.Items.Add(new ToolStripMenuItem("Exit", null, (s, e) => Exit()));

            return menu;
        }


        private void ToggleAutostart(object sender, EventArgs e)
        {
            if (AutoStart.IsRegistered())
            {
                AutoStart.Unregister();
            }
            else
            {
                AutoStart.Register();
            }
            
            RebuildMenu();
        }

        private void ApplySelectedInput()
        {
            if (MonitorHelper.Monitors.Count == 0)
            {
                MessageBox.Show("No DDC/CI capable monitors detected!\n\nPlease ensure your monitor supports DDC/CI and is properly connected.",
                    "No Monitors Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (selectedMonitor >= MonitorHelper.Monitors.Count)
            {
                MessageBox.Show($"Selected monitor (#{selectedMonitor + 1}) is no longer available.\nPlease select a different monitor.",
                    "Monitor Not Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                selectedMonitor = 0;
                SaveSettings();
                RebuildMenu();
                return;
            }
            
            bool result = MonitorHelper.SetInput(selectedMonitor, selectedInput);
            if (!result)
            {
                MessageBox.Show($"Input could not be set!\nMonitor: {selectedMonitor + 1}, Input: 0x{selectedInput:X}",
                    "DDC Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ApplySelectedInput();
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
