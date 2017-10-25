using System;

namespace Shibari.Sub.Core.Shared.Types.Common
{
    public class ChildDeviceAttachedEventArgs : EventArgs
    {
        public ChildDeviceAttachedEventArgs(IDualShockDevice device)
        {
            Device = device;
        }

        public IDualShockDevice Device { get; }
    }

    public class ChildDeviceRemovedEventArgs : EventArgs
    {
        public ChildDeviceRemovedEventArgs(IDualShockDevice device)
        {
            Device = device;
        }

        public IDualShockDevice Device { get; }
    }

    public class InputReportReceivedEventArgs : EventArgs
    {
        public InputReportReceivedEventArgs(IDualShockDevice device, IInputReport report)
        {
            Device = device;
            Report = report;
        }

        public IDualShockDevice Device { get; }

        public IInputReport Report { get; }
    }

    public delegate void ChildDeviceAttachedEventHandler(object sender, ChildDeviceAttachedEventArgs e);

    public delegate void ChildDeviceRemovedEventHandler(object sender, ChildDeviceRemovedEventArgs e);

    public delegate void InputReportReceivedEventHandler(object sender, InputReportReceivedEventArgs e);

    public interface IBusEmulator
    {
        void Start();

        void Stop();

        event ChildDeviceAttachedEventHandler ChildDeviceAttached;

        event ChildDeviceRemovedEventHandler ChildDeviceRemoved;

        event InputReportReceivedEventHandler InputReportReceived;
    }
}