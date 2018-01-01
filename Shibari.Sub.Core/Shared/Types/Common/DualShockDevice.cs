using System;
using System.Net.NetworkInformation;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shibari.Sub.Core.Shared.Types.Common
{
    public delegate void DualShockInputReportReceivedEventHandler(object sender, InputReportEventArgs e);

    public abstract class DualShockDevice : IDualShockDevice
    {
        private readonly CancellationTokenSource _inputCancellationTokenSourcePrimary = new CancellationTokenSource();
        private readonly CancellationTokenSource _inputCancellationTokenSourceSecondary = new CancellationTokenSource();
        private readonly IObservable<long> _outputReportSchedule = Observable.Interval(TimeSpan.FromMilliseconds(10));
        private readonly IDisposable _outputReportTask;

        protected DualShockDevice(DualShockDeviceType deviceType, DualShockConnectionType connectionType, int index)
        {
            DeviceType = deviceType;
            ConnectionType = connectionType;

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

        public DualShockDeviceType DeviceType { get; }
        public DualShockConnectionType ConnectionType { get; }
        public PhysicalAddress ClientAddress { get; protected set; }
        public PhysicalAddress HostAddress { get; protected set; }
        public int DeviceIndex { get; }
        protected byte[] HidOutputReport { get; }

        public event DualShockInputReportReceivedEventHandler InputReportReceived;

        public abstract void PairTo(PhysicalAddress host);
        public abstract void Rumble(byte largeMotor, byte smallMotor);

        protected abstract void RequestInputReportWorker(object cancellationToken);
        protected abstract void OnOutputReport(long l);

        protected void OnInputReport(IInputReport report)
        {
            InputReportReceived?.Invoke(this, new InputReportEventArgs(report));
        }
    }
}