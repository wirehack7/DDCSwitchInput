using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;

namespace DdcTraySwitcher
{
    internal static class MonitorHelper
    {
        public struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }

        public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport("dxva2.dll", SetLastError = true)]
        static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint count, [Out] PHYSICAL_MONITOR[] monitors);

        [DllImport("dxva2.dll", SetLastError = true)]
        static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, ref uint pdwNumberOfPhysicalMonitors);

        [DllImport("dxva2.dll", SetLastError = true)]
        static extern bool GetVCPFeatureAndVCPFeatureReply(
            IntPtr hMonitor,
            byte bVCPCode,
            out byte pvct,
            out uint currentValue,
            out uint maximumValue
        );

        [DllImport("dxva2.dll", SetLastError = true)]
        static extern bool SetVCPFeature(IntPtr hMonitor, byte vcpCode, uint newValue);

        [DllImport("dxva2.dll", SetLastError = true)]
        static extern bool DestroyPhysicalMonitors(uint count, PHYSICAL_MONITOR[] monitors);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        public static List<PHYSICAL_MONITOR> Monitors { get; } = new();

        public static void Initialize()
        {
            Monitors.Clear();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnum, IntPtr.Zero);
        }

        private static bool MonitorEnum(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data)
        {
            uint count = 0;
            if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, ref count) || count == 0)
                return true;

            var physical = new PHYSICAL_MONITOR[count];

            if (GetPhysicalMonitorsFromHMONITOR(hMonitor, count, physical))
            {
                foreach (var mon in physical)
                {
                    if (IsDdcCapable(mon.hPhysicalMonitor))
                        Monitors.Add(mon);
                    else
                        DestroyPhysicalMonitors(1, new[] { mon });
                }
            }

            return true;
        }

        public static bool SetInput(int monitorIndex, uint input)
        {
            try
            {
                if (monitorIndex < 0 || monitorIndex >= Monitors.Count)
                    return false;

                var h = Monitors[monitorIndex].hPhysicalMonitor;
                bool success = SetVCPFeature(h, 0x60, input);

                return success;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"DDC/CI error: {ex.Message}", "DDC error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static void Dispose()
        {
            foreach (var m in Monitors)
            {
                DestroyPhysicalMonitors(1, new[] { m });
            }

            Monitors.Clear();
        }
        private static bool IsDdcCapable(IntPtr hMonitor)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
            var token = cts.Token;

            Task<bool> task = Task.Run(() =>
            {
                try
                {
                    return GetVCPFeatureAndVCPFeatureReply(hMonitor, 0x60, out _, out _, out _);
                }
                catch
                {
                    return false;
                }
            }, token);

            try
            {
                return task.Wait(300, token) && task.Result;
            }
            catch
            {
                return false;
            }
        }
    }
}
