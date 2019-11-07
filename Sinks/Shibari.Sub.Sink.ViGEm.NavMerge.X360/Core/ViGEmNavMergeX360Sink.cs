using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.ExceptionServices;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Shared.Types.Common.Sinks;
using Shibari.Sub.Core.Shared.Types.DualShock3;

namespace Shibari.Sub.Sink.ViGEm.NavMerge.X360.Core
{
    internal class ಠ_ಠAttribute : Attribute
    {
    }

    [ExportMetadata("Name", "ViGEm Dual Navigation to Xbox 360 Sink")]
    [Export(typeof(ISinkPlugin))]
    public class ViGEmNavMergeX360Sink : ISinkPlugin
    {
        private readonly Dictionary<DualShock3Buttons, Xbox360Button> _btnMap;
        private readonly ViGEmClient _client;

        private readonly Dictionary<IDualShockDevice, IXbox360Controller> _deviceMap =
            new Dictionary<IDualShockDevice, IXbox360Controller>();

        private readonly Dictionary<DualShock3Axes, Xbox360Slider> _triggerAxisMap;
        private readonly Dictionary<DualShock3Axes, Xbox360Axis> _XaxisMap;
        private readonly Dictionary<DualShock3Axes, Xbox360Axis> _YaxisMap;

        private static DualShock3InputReport TempReport;
        private bool firstReport = false; //Hack: fix me

        private readonly System.Diagnostics.Stopwatch[] Stopwatchs = { new System.Diagnostics.Stopwatch(), new System.Diagnostics.Stopwatch(), new System.Diagnostics.Stopwatch(), new System.Diagnostics.Stopwatch() };
        private TimeSpan[] timeSpans = new TimeSpan[4];
        //These are hacky as well

        public ViGEmNavMergeX360Sink()
        {
            _btnMap = new Dictionary<DualShock3Buttons, Xbox360Button>
            {
                {DualShock3Buttons.Select, Xbox360Button.Back},
                {DualShock3Buttons.LeftThumb, Xbox360Button.LeftThumb},
                {DualShock3Buttons.RightThumb, Xbox360Button.RightThumb},
                {DualShock3Buttons.Start, Xbox360Button.Start},
                {DualShock3Buttons.LeftShoulder, Xbox360Button.LeftShoulder},
                {DualShock3Buttons.RightShoulder, Xbox360Button.RightShoulder},
                {DualShock3Buttons.Triangle, Xbox360Button.Y},
                {DualShock3Buttons.Circle, Xbox360Button.B},
                {DualShock3Buttons.Cross, Xbox360Button.A},
                {DualShock3Buttons.Square, Xbox360Button.X},
                {DualShock3Buttons.DPadUp, Xbox360Button.Up},
                {DualShock3Buttons.DPadDown, Xbox360Button.Down},
                {DualShock3Buttons.DPadLeft, Xbox360Button.Left},
                {DualShock3Buttons.DPadRight, Xbox360Button.Right},
                {DualShock3Buttons.Ps, Xbox360Button.Guide}
            };

            _XaxisMap = new Dictionary<DualShock3Axes, Xbox360Axis>
            {
                {DualShock3Axes.LeftThumbX, Xbox360Axis.LeftThumbX},
                {DualShock3Axes.RightThumbX, Xbox360Axis.RightThumbX}
            };

            _YaxisMap = new Dictionary<DualShock3Axes, Xbox360Axis>
            {
                {DualShock3Axes.LeftThumbY, Xbox360Axis.LeftThumbY},
                {DualShock3Axes.RightThumbY, Xbox360Axis.RightThumbY}
            };

            _triggerAxisMap = new Dictionary<DualShock3Axes, Xbox360Slider>
            {
                {DualShock3Axes.LeftTrigger, Xbox360Slider.LeftTrigger},
                {DualShock3Axes.RightTrigger, Xbox360Slider.RightTrigger}
            };

            _client = new ViGEmClient();
        }

        public event RumbleRequestReceivedEventHandler RumbleRequestReceived;

        [HandleProcessCorruptedStateExceptions]
        public void DeviceArrived(IDualShockDevice device)
        {
            if (device.DeviceIndex != 1)
            {
                var target = _client.CreateXbox360Controller();
                target.AutoSubmitReport = false;

                _deviceMap.Add(device, target);

                target.FeedbackReceived += (sender, args) =>
                {
                    var source = _deviceMap.First(m => m.Value.Equals(sender)).Key;

                    RumbleRequestReceived?.Invoke(source, new RumbleRequestEventArgs(args.LargeMotor, args.SmallMotor));
                };

                try
                {
                    Log.Information("Connecting ViGEm target: {Target}", target);
                    target.Connect();
                    Log.Information("ViGEm target {Target} connected successfully", target);
                    Log.Debug("Device Path {Path}, index {Index}", device.DevicePath, device.DeviceIndex);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to connect target {@Target}: {Exception}", target, ex);
                }
            }
            Stopwatchs[device.DeviceIndex].Start();
        }

        [HandleProcessCorruptedStateExceptions]
        public void DeviceRemoved(IDualShockDevice device)
        {
            if (device.DeviceIndex != 1)
            {
                _deviceMap[device].Disconnect();
                _deviceMap.Remove(device);
            }

            if (Stopwatchs[device.DeviceIndex].IsRunning)
            {
                Stopwatchs[device.DeviceIndex].Stop();
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public void InputReportReceived(IDualShockDevice device, IInputReport report)
        {
            switch (device.DeviceType)
            {
                case DualShockDeviceType.DualShock3:

                    timeSpans[device.DeviceIndex] = Stopwatchs[device.DeviceIndex].Elapsed;

                    Stopwatchs[device.DeviceIndex].Restart();

                    var ds3Report = (DualShock3InputReport)report;
                    Log.Debug("Device {Index} report recieved, difference of {Diff} MilliSec from last", device.DeviceIndex, timeSpans[device.DeviceIndex].TotalMilliseconds);

                    if (device.DeviceIndex == 1)
                    {
                        TempReport = ds3Report;
                    }
                    else
                    {

                        var target = _deviceMap[device];

                        if (device.DeviceIndex == 0)
                        {
                            MergeRight(device, ds3Report);
                        }

                        target.ResetReport();

                        foreach (var axis in _XaxisMap) target.SetAxisValue(axis.Value, Scale(ds3Report[axis.Key], false));

                        foreach (var axis in _YaxisMap) target.SetAxisValue(axis.Value, Scale(ds3Report[axis.Key], true));

                        foreach (var axis in _triggerAxisMap) target.SetSliderValue(axis.Value, ds3Report[axis.Key]);

                        foreach (var button in _btnMap.Where(m => ds3Report.EngagedButtons.Contains(m.Key))
                            .Select(m => m.Value)) target.SetButtonState(button, true);

                        target.SubmitReport();
                    }
                    break;
            }
        }

        [ಠ_ಠ]
        private static short Scale(byte value, bool invert)
        {
            var intValue = value - 0x80;
            if (intValue == -128) intValue = -127;

            var wtfValue = intValue * 258.00787401574803149606299212599f; // what the fuck?

            return (short)(invert ? -wtfValue : wtfValue);
        }

        private static void MergeRight(IDualShockDevice device, DualShock3InputReport ds3Report)
        {

            //TODO: finish
            var buttons = (uint)((ds3Report.Buffer[2] << 0)
                                | (ds3Report.Buffer[3] << 8)
                                | (ds3Report.Buffer[4] << 16)
                                | (ds3Report.Buffer[5] << 24));

            //Map over buttons (not done!)
            TempReport.Buffer[2] = ds3Report.Buffer[2];
            TempReport.Buffer[3] = ds3Report.Buffer[3];
            TempReport.Buffer[4] = ds3Report.Buffer[4];
            TempReport.Buffer[5] = ds3Report.Buffer[5];

            //Map axes (not done!)
            TempReport.Buffer[8] = ds3Report.Buffer[6];
            TempReport.Buffer[9] = ds3Report.Buffer[7];
            TempReport.Buffer[19] = ds3Report.Buffer[18];
        }
    }
}