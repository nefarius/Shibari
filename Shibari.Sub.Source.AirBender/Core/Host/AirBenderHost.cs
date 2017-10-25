using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using PInvoke;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Util;
using Shibari.Sub.Source.AirBender.Core.Children;
using Shibari.Sub.Source.AirBender.Core.Children.DualShock3;
using Shibari.Sub.Source.AirBender.Exceptions;

namespace Shibari.Sub.Source.AirBender.Core.Host
{
    internal delegate void HostDeviceDisconnectedEventHandler(object sender, EventArgs e);

    /// <summary>
    ///     Represents a managed wrapper around an USB device loaded with the AirBender driver.
    /// </summary>
    internal sealed partial class AirBenderHost : IDisposable
    {
        private readonly IObservable<long> _deviceLookupSchedule = Observable.Interval(TimeSpan.FromSeconds(2));
        private readonly IDisposable _deviceLookupTask;

        /// <summary>
        ///     Opens an AirBender device by its device path.
        /// </summary>
        /// <param name="devicePath">The device path to open.</param>
        public AirBenderHost(string devicePath)
        {
            DevicePath = devicePath;
            Children = new ObservableCollection<AirBenderChildDevice>();

            /*foreach (var plugin in _sinkPluginHost.SinkPlugins)
            {
                Log.Information($"Loaded plugin {plugin.Metadata["PluginName"]}");
            }

            Children.CollectionChanged += (sender, args) =>
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:

                        foreach (IDualShockDevice item in args.NewItems)
                            _sinkPluginHost.DeviceArrived(item);

                        break;
                    case NotifyCollectionChangedAction.Remove:

                        foreach (IDualShockDevice item in args.OldItems)
                            _sinkPluginHost.DeviceRemoved(item);

                        break;
                    default:
                        break;
                }
            };*/

            //
            // Open device
            // 
            DeviceHandle = Kernel32.CreateFile(DevicePath,
                Kernel32.ACCESS_MASK.GenericRight.GENERIC_READ | Kernel32.ACCESS_MASK.GenericRight.GENERIC_WRITE,
                Kernel32.FileShare.FILE_SHARE_READ | Kernel32.FileShare.FILE_SHARE_WRITE,
                IntPtr.Zero, Kernel32.CreationDisposition.OPEN_EXISTING,
                Kernel32.CreateFileFlags.FILE_ATTRIBUTE_NORMAL | Kernel32.CreateFileFlags.FILE_FLAG_OVERLAPPED,
                Kernel32.SafeObjectHandle.Null
            );

            if (DeviceHandle.IsInvalid)
                throw new ArgumentException($"Couldn't open device {DevicePath}");

            var length = Marshal.SizeOf(typeof(AirbenderGetHostBdAddr));
            var pData = Marshal.AllocHGlobal(length);
            var bytesReturned = 0;
            bool ret;

            try
            {
                //
                // Request host MAC address
                // 
                ret = DeviceHandle.OverlappedDeviceIoControl(
                    IoctlAirbenderGetHostBdAddr,
                    IntPtr.Zero, 0, pData, length,
                    out bytesReturned);

                if (!ret)
                    throw new AirBenderGetHostBdAddrFailedException();

                HostAddress =
                    new PhysicalAddress(Marshal.PtrToStructure<AirbenderGetHostBdAddr>(pData).Host.Address.Reverse()
                        .ToArray());
            }
            finally
            {
                Marshal.FreeHGlobal(pData);
            }

            Log.Information($"Bluetooth Host Address: {HostAddress.AsFriendlyName()}");

            //
            // Request host controller to reset and clean up resources
            // 
            ret = DeviceHandle.OverlappedDeviceIoControl(
                IoctlAirbenderHostReset,
                IntPtr.Zero, 0, IntPtr.Zero, 0,
                out bytesReturned);

            if (!ret)
                throw new AirBenderHostResetFailedException();

            _deviceLookupTask = _deviceLookupSchedule.Subscribe(OnLookup);
        }

        /// <summary>
        ///     Native handle to device.
        /// </summary>
        public Kernel32.SafeObjectHandle DeviceHandle { get; }

        /// <summary>
        ///     Child devices attached to this AirBender device.
        /// </summary>
        private ObservableCollection<AirBenderChildDevice> Children { get; }

        /// <summary>
        ///     Class GUID identifying an AirBender device.
        /// </summary>
        public static Guid ClassGuid => Guid.Parse("a775e97e-a41b-4bfc-868e-25be84643b62");

        /// <summary>
        ///     The device path.
        /// </summary>
        public string DevicePath { get; }

        /// <summary>
        ///     MAC address of this Bluetooth host.
        /// </summary>
        public PhysicalAddress HostAddress { get; }

        /// <summary>
        ///     Gets fired when the host device gets disconnected or become inaccessible due to an error.
        /// </summary>
        public event HostDeviceDisconnectedEventHandler HostDeviceDisconnected;

        /// <summary>
        ///     Gets called periodically to determine new child devices.  
        /// </summary>
        /// <param name="l">Lookup interval.</param>
        private void OnLookup(long l)
        {
            var length = Marshal.SizeOf(typeof(AirbenderGetClientCount));
            var pData = Marshal.AllocHGlobal(length);

            try
            {
                //
                // Request client count
                // 
                var bytesReturned = 0;
                var ret = DeviceHandle.OverlappedDeviceIoControl(
                    IoctlAirbenderGetClientCount,
                    IntPtr.Zero, 0, pData, length,
                    out bytesReturned);

                //
                // This happens if the host device got "surprise-removed"
                // 
                if (!ret && Marshal.GetLastWin32Error() == ErrorBadCommand)
                {
                    Log.Warning($"Connection to device {DevicePath} lost, possibly it got removed");

                    HostDeviceDisconnected?.Invoke(this, EventArgs.Empty);

                    return;
                }

                //
                // Here something terrible happened
                // 
                if (!ret)
                {
                    Log.Error($"Unexpected error: {new Win32Exception(Marshal.GetLastWin32Error())}");
                    return;
                }

                var count = Marshal.PtrToStructure<AirbenderGetClientCount>(pData).Count;

                //
                // Return if no children or all children are already known
                // 
                if (count == 0 || count == Children.Count) return;

                Log.Information($"Currently connected devices: {count}");

                for (uint i = 0; i < count; i++)
                {
                    // TODO: implement more checks, this could accidentally register the same devices again

                    PhysicalAddress address;
                    DualShockDeviceType type;

                    if (!GetDeviceStateByIndex(i, out address, out type))
                    {
                        Log.Warning(
                            $"Failed to request details for client #{i}: {new Win32Exception(Marshal.GetLastWin32Error())}");
                        continue;
                    }

                    switch (type)
                    {
                        case DualShockDeviceType.DualShock3:
                            var device = new AirBenderDualShock3(this, address, (int) i);

                            device.ChildDeviceDisconnected +=
                                (sender, args) => Children.Remove((AirBenderChildDevice) sender);
                            //device.InputReportReceived +=
                            //    (sender, args) => _sinkPluginHost.InputReportReceived((AirBenderChildDevice) sender,
                            //        args.Report);

                            Children.Add(device);

                            break;
                        case DualShockDeviceType.DualShock4:
                            throw new NotImplementedException();
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pData);
            }
        }

        /// <summary>
        ///     Requests the type and unique address of the device from the host based on the devices index.
        /// </summary>
        /// <param name="clientIndex">The child device index (zero-based).</param>
        /// <param name="address">The child device MAC address.</param>
        /// <param name="type">The type of the child device.</param>
        /// <returns>True on success, false otherwise.</returns>
        private bool GetDeviceStateByIndex(uint clientIndex, out PhysicalAddress address, out DualShockDeviceType type)
        {
            var requestSize = Marshal.SizeOf<AirbenderGetClientDetails>();
            var requestBuffer = Marshal.AllocHGlobal(requestSize);

            try
            {
                //
                // Identifier is the index
                // 
                Marshal.StructureToPtr(
                    new AirbenderGetClientDetails
                    {
                        ClientIndex = clientIndex
                    },
                    requestBuffer, false);

                int bytesReturned;
                var ret = DeviceHandle.OverlappedDeviceIoControl(
                    IoctlAirbenderGetClientState,
                    requestBuffer, requestSize, requestBuffer, requestSize,
                    out bytesReturned);

                if (!ret && Marshal.GetLastWin32Error() == ErrorDevNotExist)
                    throw new AirBenderDeviceNotFoundException();

                if (ret)
                {
                    var resp = Marshal.PtrToStructure<AirbenderGetClientDetails>(requestBuffer);

                    type = resp.DeviceType;
                    address = new PhysicalAddress(resp.ClientAddress.Address.Reverse().ToArray());

                    return true;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(requestBuffer);
            }

            type = DualShockDeviceType.Unknown;
            address = PhysicalAddress.None;

            return false;
        }

        #region Equals Support

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as Shibari.Sub.Source.AirBender.Core.Host.AirBenderHost;
            return other != null && Equals(other);
        }

        private bool Equals(Shibari.Sub.Source.AirBender.Core.Host.AirBenderHost other)
        {
            return string.Equals(DevicePath, other.DevicePath, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(DevicePath);
        }

        public static bool operator ==(Shibari.Sub.Source.AirBender.Core.Host.AirBenderHost left, Shibari.Sub.Source.AirBender.Core.Host.AirBenderHost right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Shibari.Sub.Source.AirBender.Core.Host.AirBenderHost left, Shibari.Sub.Source.AirBender.Core.Host.AirBenderHost right)
        {
            return !Equals(left, right);
        }

        #endregion

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _deviceLookupTask?.Dispose();

                    foreach (var child in Children)
                        child.Dispose();

                    Children.Clear();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                int bytesReturned;
                DeviceHandle.OverlappedDeviceIoControl(
                    IoctlAirbenderHostShutdown,
                    IntPtr.Zero, 0, IntPtr.Zero, 0, out bytesReturned);

                DeviceHandle?.Close();

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~AirBenderHost()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}