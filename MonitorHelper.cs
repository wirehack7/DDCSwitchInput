using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
            const uint count = 1;
            var physical = new PHYSICAL_MONITOR[count];

            if (GetPhysicalMonitorsFromHMONITOR(hMonitor, count, physical))
            {
                Monitors.AddRange(physical);
            }

            return true; // continue enumeration
        }

        public static bool SetInput(int monitorIndex, uint input)
        {
            if (monitorIndex < 0 || monitorIndex >= Monitors.Count)
                return false;

            var h = Monitors[monitorIndex].hPhysicalMonitor;
            return SetVCPFeature(h, 0x60, input);
        }

        public static void Dispose()
        {
            foreach (var m in Monitors)
            {
                DestroyPhysicalMonitors(1, new[] { m });
            }

            Monitors.Clear();
        }
    }
}
