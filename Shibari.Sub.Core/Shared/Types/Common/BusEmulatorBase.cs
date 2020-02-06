using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Threading;
using Shibari.Sub.Core.Shared.Types.Common.Sources;

namespace Shibari.Sub.Core.Shared.Types.Common
{
    public abstract class BusEmulatorBase : SourcePluginBase, IBusEmulator
    {
        private readonly IObservable<long> _deviceLookupSchedule = Observable.Interval(TimeSpan.FromSeconds(2));

        protected readonly ObservableCollection<DualShockDevice> ChildDevices =
            new ObservableCollection<DualShockDevice>();

        private IDisposable _deviceLookupTask;

        public string Name => GetType().Name;

        public abstract BusEmulatorConnectionType ConnectionType { get; }

        public virtual void Start()
        {
            ChildDevices.CollectionChanged += (sender, args) =>
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
                }
            };

            _deviceLookupTask = _deviceLookupSchedule.Subscribe(OnLookup);
            OnLookup(0);
        }

        public virtual void Stop()
        {
            _deviceLookupTask?.Dispose();

            // TODO: race condition, can raise exception
            foreach (var device in ChildDevices)
                device.Dispose();

            ChildDevices.Clear();
        }

        public event ChildDeviceAttachedEventHandler ChildDeviceAttached;
        public event ChildDeviceRemovedEventHandler ChildDeviceRemoved;
        public event InputReportReceivedEventHandler InputReportReceived;

        protected void OnInputReportReceived(IDualShockDevice sender, IInputReport report)
        {
            InputReportReceived?.Invoke(this,
                new InputReportReceivedEventArgs(sender, report));
        }

        private void OnLookup(long l)
        {
            if (!Monitor.TryEnter(_deviceLookupTask))
                return;

            try
            {
                OnLookup();
            }
            finally
            {
                Monitor.Exit(_deviceLookupTask);
            }
        }

        protected abstract void OnLookup();

        /// <summary>
        ///     Friendly name of this bus emulator.
        /// </summary>
        /// <returns>The friendly name of this bus emulator.</returns>
        public override string ToString()
        {
            return Name;
        }
    }
}