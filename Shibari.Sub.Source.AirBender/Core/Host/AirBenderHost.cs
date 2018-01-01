using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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

    /// <inheritdoc />
    /// <summary>
    ///     Represents a managed wrapper around an USB device loaded with the AirBender driver.
    /// </summary>
    internal sealed partial class AirBenderHost : IDisposable
    {
        private readonly CancellationTokenSource _arrivalCancellationTokenSourcePrimary = new CancellationTokenSource();

        private readonly CancellationTokenSource _arrivalCancellationTokenSourceSecondary =
            new CancellationTokenSource();

        private readonly CancellationTokenSource _removalCancellationTokenSourcePrimary = new CancellationTokenSource();

        private readonly CancellationTokenSource _removalCancellationTokenSourceSecondary =
            new CancellationTokenSource();

        /// <summary>
        ///     Opens an AirBender device by its device path.
        /// </summary>
        /// <param name="devicePath">The device path to open.</param>
        public AirBenderHost(string devicePath)
        {
            DevicePath = devicePath;
            Children = new ObservableCollection<AirBenderChildDevice>();

            Children.CollectionChanged += (sender, args) =>
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:

                        foreach (IDualShockDevice item in args.NewItems)
                            ChildDeviceAttached?.Invoke(this, new ChildDeviceAttachedEventArgs(item));

                        break;
                    case NotifyCollectionChangedAction.Remove:

                        foreach (IDualShockDevice item in args.OldItems)
                            ChildDeviceRemoved?.Invoke(this, new ChildDeviceRemovedEventArgs(item));

                        break;
                    default:
                        break;
                }
            };

            //
            // Open device
            // 
            DeviceHandle = Kernel32.CreateFile(DevicePath,
                Kernel32.ACCESS_MASK.GenericRight.GENERIC_READ | Kernel32.ACCESS_MASK.GenericRight.GENERIC_WRITE,
                Kernel32.FileShare.FILE_SHARE_READ | Kernel32.FileShare.FILE_SHARE_WRITE,
                IntPtr.Zero, Kernel32.CreationDisposition.OPEN_EXISTING,
                Kernel32.CreateFileFlags.FILE_ATTRIBUTE_NORMAL 
                | Kernel32.CreateFileFlags.FILE_FLAG_NO_BUFFERING
                | Kernel32.CreateFileFlags.FILE_FLAG_WRITE_THROUGH
                | Kernel32.CreateFileFlags.FILE_FLAG_OVERLAPPED,
                Kernel32.SafeObjectHandle.Null
            );

            if (DeviceHandle.IsInvalid)
                throw new ArgumentException($"Couldn't open device {DevicePath}");

            var length = Marshal.SizeOf(typeof(AirbenderGetHostBdAddr));
            var pData = Marshal.AllocHGlobal(length);
            bool ret;

            try
            {
                //
                // Request host MAC address
                // 
                ret = DeviceHandle.OverlappedDeviceIoControl(
                    IoctlAirbenderGetHostBdAddr,
                    IntPtr.Zero, 0, pData, length,
                    out _);

                if (!ret)
                    throw new AirBenderGetHostBdAddrFailedException(
                        "Failed to request host MAC address.", 
                        new Win32Exception(Marshal.GetLastWin32Error()));

                HostAddress =
                    new PhysicalAddress(Marshal.PtrToStructure<AirbenderGetHostBdAddr>(pData).Host.Address.Reverse()
                        .ToArray());
            }
            finally
            {
                Marshal.FreeHGlobal(pData);
            }

            Log.Information("Bluetooth Host Address: {HostAddress}", HostAddress.AsFriendlyName());

            //
            // Request host controller to reset and clean up resources
            // 
            ret = DeviceHandle.OverlappedDeviceIoControl(
                IoctlAirbenderHostReset,
                IntPtr.Zero, 0, IntPtr.Zero, 0,
                out _);

            if (!ret)
                throw new AirBenderHostResetFailedException(
                    "Failed to reset host.",
                    new Win32Exception(Marshal.GetLastWin32Error()));

            Task.Factory.StartNew(ChildDeviceArrivalWorker, _arrivalCancellationTokenSourcePrimary.Token);
            Task.Factory.StartNew(ChildDeviceArrivalWorker, _arrivalCancellationTokenSourceSecondary.Token);

            Task.Factory.StartNew(ChildDeviceRemovalWorker, _removalCancellationTokenSourcePrimary.Token);
            Task.Factory.StartNew(ChildDeviceRemovalWorker, _removalCancellationTokenSourceSecondary.Token);
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

        private void ChildDeviceArrivalWorker(object cancellationToken)
        {
            var token = (CancellationToken)cancellationToken;
            var requestSize = Marshal.SizeOf<AirbenderGetClientArrival>();
            var requestBuffer = Marshal.AllocHGlobal(requestSize);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    //
                    // This call blocks until the driver supplies new data.
                    //  
                    var ret = DeviceHandle.OverlappedDeviceIoControl(
                        IoctlAirbenderGetClientArrival,
                        IntPtr.Zero, 0, requestBuffer, requestSize,
                        out _);

                    if (!ret)
                        throw new AirbenderGetClientArrivalFailedException(
                            "Failed to receive device arrival event.",
                            new Win32Exception(Marshal.GetLastWin32Error()));

                    var resp = Marshal.PtrToStructure<AirbenderGetClientArrival>(requestBuffer);

                    switch (resp.DeviceType)
                    {
                        case DualShockDeviceType.DualShock3:
                            var device = new AirBenderDualShock3(
                                this,
                                new PhysicalAddress(resp.ClientAddress.Address.Reverse().ToArray()),
                                Children.Count);

                            device.InputReportReceived +=
                                (sender, args) => InputReportReceived?.Invoke(this,
                                    new InputReportReceivedEventArgs((IDualShockDevice)sender, args.Report));

                            Children.Add(device);

                            break;
                        case DualShockDeviceType.DualShock4:
                            throw new NotImplementedException();
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(requestBuffer);
            }
        }

        private void ChildDeviceRemovalWorker(object cancellationToken)
        {
            var token = (CancellationToken)cancellationToken;
            var requestSize = Marshal.SizeOf<AirbenderGetClientRemoval>();
            var requestBuffer = Marshal.AllocHGlobal(requestSize);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    //
                    // This call blocks until the driver supplies new data.
                    //  
                    var ret = DeviceHandle.OverlappedDeviceIoControl(
                        IoctlAirbenderGetClientRemoval,
                        IntPtr.Zero, 0, requestBuffer, requestSize,
                        out _);

                    if (!ret)
                        throw new AirBenderGetClientRemovalFailedException(
                            "Failed to receive device removal event.",
                            new Win32Exception(Marshal.GetLastWin32Error()));

                    var resp = Marshal.PtrToStructure<AirbenderGetClientRemoval>(requestBuffer);

                    var child = Children.First(c =>
                        c.ClientAddress.Equals(new PhysicalAddress(resp.ClientAddress.Address.Reverse().ToArray())));

                    child.Dispose();
                    Children.Remove(child);
                }
            }
            catch (Exception ex)
            {
                Log.Error("{Exception}", ex);
            }
            finally
            {
                Marshal.FreeHGlobal(requestBuffer);
            }
        }

        /// <summary>
        ///     Gets fired when the host device gets disconnected or become inaccessible due to an error.
        /// </summary>
        public event HostDeviceDisconnectedEventHandler HostDeviceDisconnected;

        public event ChildDeviceAttachedEventHandler ChildDeviceAttached;

        public event ChildDeviceRemovedEventHandler ChildDeviceRemoved;

        public event InputReportReceivedEventHandler InputReportReceived;

        #region Equals Support

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as AirBenderHost;
            return other != null && Equals(other);
        }

        private bool Equals(AirBenderHost other)
        {
            return string.Equals(DevicePath, other.DevicePath, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(DevicePath);
        }

        public static bool operator ==(AirBenderHost left, AirBenderHost right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AirBenderHost left, AirBenderHost right)
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
                    _arrivalCancellationTokenSourcePrimary.Cancel();
                    _arrivalCancellationTokenSourceSecondary.Cancel();

                    _removalCancellationTokenSourcePrimary.Cancel();
                    _removalCancellationTokenSourceSecondary.Cancel();

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