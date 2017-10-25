namespace Shibari.Sub.Core.Shared.Types.Common.Sinks
{
    public interface ISinkPlugin
    {
        void DeviceArrived(IDualShockDevice device);

        void DeviceRemoved(IDualShockDevice device);

        void InputReportReceived(IDualShockDevice device, IInputReport report);
    }
}