using System;
using System.Net.NetworkInformation;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using PInvoke;

namespace Shibari.Sub.Core.Shared.Types.Common
{
    public delegate void DualShockInputReportReceivedEventHandler(object sender, InputReportEventArgs e);

    public abstract class DualShockDevice : IDualShockDevice, IDisposable
    {
        private readonly CancellationTokenSource _inputCancellationTokenSourcePrimary = new CancellationTokenSource();
        private readonly CancellationTokenSource _inputCancellationTokenSourceSecondary = new CancellationTokenSource();
        private readonly IObservable<long> _outputReportSchedule = Observable.Interval(TimeSpan.FromMilliseconds(10));
        private readonly IDisposable _outputReportTask;
        protected IntPtr OutputReportBuffer { get; }

        protected DualShockDevice(DualShockConnectionType connectionType, Kernel32.SafeObjectHandle handle, int index)
        {
            ConnectionType = connectionType;
            DeviceHandle = handle;
            DeviceIndex = index;

            OutputReportBuffer = Marshal.AllocHGlobal(HidOutputReport.Length);
            _outputReportTask = _outputReportSchedule.Subscribe(OnOutputReport);

            //
            // Start two tasks requesting input reports in parallel.
            // 
            // While on threads request gets completed, another request can be
            // queued by the other thread. This way no input can get lost because
            // there's always at least one pending request in the driver to get
            // completed. Each thread uses inverted calls for maximum performance.
            // 
            Task.Factory.StartNew(RequestInputReportWorker, _inputCancellationTokenSourcePrimary.Token);
            Task.Factory.StartNew(RequestInputReportWorker, _inputCancellationTokenSourceSecondary.Token);
        }

        /// <summary>
        ///     Native handle to device.
        /// </summary>
        protected Kernel32.SafeObjectHandle DeviceHandle { get; }

        /// <summary>
        ///     The <see cref="DualShockDeviceType"/> of the current device.
        /// </summary>
        public DualShockDeviceType DeviceType { get; protected set; }

        /// <summary>
        ///     The <see cref="DualShockConnectionType"/> of this device.
        /// </summary>
        public DualShockConnectionType ConnectionType { get; }

        /// <summary>
        ///     The Bluetooth MAC address of this device.
        /// </summary>
        public PhysicalAddress ClientAddress { get; protected set; }

        /// <summary>
        ///     Host MAC address this device is paired to.
        /// </summary>
        public PhysicalAddress HostAddress { get; protected set; }

        public int DeviceIndex { get; }

        public string DevicePath { get; protected set; }

        protected abstract byte[] HidOutputReport { get; }

        public event DualShockInputReportReceivedEventHandler InputReportReceived;

        /// <summary>
        ///     Pairs the current device to the specified host via its address.
        /// </summary>
        /// <param name="host">The address to pair to.</param>
        public abstract void PairTo(PhysicalAddress host);

        /// <summary>
        ///     Send Rumble request to the controller.
        /// </summary>
        /// <param name="largeMotor">Large motor intensity (0 = off, 255 = max).</param>
        /// <param name="smallMotor">Small motor intensity (0 = off, >0 = on).</param>
        public abstract void Rumble(byte largeMotor, byte smallMotor);

        /// <summary>
        ///     Worker thread requesting HID input reports.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to shutdown the worker.</param>
        protected abstract void RequestInputReportWorker(object cancellationToken);

        protected abstract void OnOutputReport(long l);

        protected void OnInputReport(IInputReport report)
        {
            InputReportReceived?.Invoke(this, new InputReportEventArgs(report));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _inputCancellationTokenSourcePrimary.Cancel();
                    _inputCancellationTokenSourceSecondary.Cancel();
                    _outputReportTask.Dispose();
                }

                Marshal.FreeHGlobal(OutputReportBuffer);

                disposedValue = true;
            }
        }
        
        ~DualShockDevice() {
          // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
          Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}