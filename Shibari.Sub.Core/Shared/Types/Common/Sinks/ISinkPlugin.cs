using System;

namespace Shibari.Sub.Core.Shared.Types.Common.Sinks
{
    public class RumbleRequestEventArgs : EventArgs
    {
        public RumbleRequestEventArgs(byte largeMotor, byte smallMotor)
        {
            LargeMotor = largeMotor;
            SmallMotor = smallMotor;
        }

        public byte LargeMotor { get; }

        public byte SmallMotor { get; }
    }

    public delegate void RumbleRequestReceivedEventHandler(object sender, RumbleRequestEventArgs e);

    public interface ISinkPlugin
    {
        void DeviceArrived(IDualShockDevice device);

        void DeviceRemoved(IDualShockDevice device);

        void InputReportReceived(IDualShockDevice device, IInputReport report);

        event RumbleRequestReceivedEventHandler RumbleRequestReceived;
    }
}