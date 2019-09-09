#define DS4

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Shared.Types.Common.Sinks;
using Shibari.Sub.Core.Shared.Types.DualShock3;

namespace Shibari.Sub.Sink.ViGEm.Core
{
#if DS4
    [ExportMetadata("Name", "ViGEm Sink")]
    [Export(typeof(ISinkPlugin))]
    public class ViGEmSink : ISinkPlugin
    {
        private readonly Dictionary<DualShock3Buttons, DualShock4Button> _btnMap;
        private readonly ViGEmClient _client;

        private readonly Dictionary<IDualShockDevice, IDualShock4Controller> _deviceMap =
            new Dictionary<IDualShockDevice, IDualShock4Controller>();

        private readonly Dictionary<DualShock3Axes, DualShock4Slider> _triggerAxisMap;
        private readonly Dictionary<DualShock3Axes, DualShock4Axis> _XaxisMap;
        private readonly Dictionary<DualShock3Axes, DualShock4Axis> _YaxisMap;

        public ViGEmSink()
        {
            _btnMap = new Dictionary<DualShock3Buttons, DualShock4Button>
            {
                {DualShock3Buttons.Select, DualShock4Button.Share},
                {DualShock3Buttons.LeftThumb, DualShock4Button.ThumbLeft},
                {DualShock3Buttons.RightThumb, DualShock4Button.ThumbRight},
                {DualShock3Buttons.Start, DualShock4Button.Options},
                {DualShock3Buttons.LeftTrigger, DualShock4Button.TriggerLeft},
                {DualShock3Buttons.RightTrigger, DualShock4Button.TriggerRight},
                {DualShock3Buttons.LeftShoulder, DualShock4Button.ShoulderLeft},
                {DualShock3Buttons.RightShoulder, DualShock4Button.ShoulderRight},
                {DualShock3Buttons.Triangle, DualShock4Button.Triangle},
                {DualShock3Buttons.Circle, DualShock4Button.Circle},
                {DualShock3Buttons.Cross, DualShock4Button.Cross},
                {DualShock3Buttons.Square, DualShock4Button.Square}
            };

            _XaxisMap = new Dictionary<DualShock3Axes, DualShock4Axis>
            {
                {DualShock3Axes.LeftThumbX, DualShock4Axis.LeftThumbX},
                {DualShock3Axes.RightThumbX, DualShock4Axis.RightThumbX}
            };

            _YaxisMap = new Dictionary<DualShock3Axes, DualShock4Axis>
            {
                {DualShock3Axes.LeftThumbY, DualShock4Axis.LeftThumbY},
                {DualShock3Axes.RightThumbY, DualShock4Axis.RightThumbY}
            };

            _triggerAxisMap = new Dictionary<DualShock3Axes, DualShock4Slider>
            {
                {DualShock3Axes.LeftTrigger, DualShock4Slider.LeftTrigger},
                {DualShock3Axes.RightTrigger, DualShock4Slider.RightTrigger}
            };

            _client = new ViGEmClient();
        }

        public event RumbleRequestReceivedEventHandler RumbleRequestReceived;

        public void DeviceArrived(IDualShockDevice device)
        {
            Log.Information("Device {Device} got attached", device);

            var target = _client.CreateDualShock4Controller();
            target.AutoSubmitReport = false;

            _deviceMap.Add(device, target);

            target.FeedbackReceived += (sender, args) =>
            {
                var source = _deviceMap.First(m => m.Value.Equals(sender)).Key;

                RumbleRequestReceived?.Invoke(source, new RumbleRequestEventArgs(args.LargeMotor, args.SmallMotor));
            };

            try
            {
                Log.Information("Connecting ViGEm target {Target}", target);
                target.Connect();
                Log.Information("ViGEm target {Target} connected successfully", target);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to connect target {@Target}: {Exception}", target, ex);
            }
        }

        public void DeviceRemoved(IDualShockDevice device)
        {
            Log.Information("Device {Device} got removed", device);

            _deviceMap[device].Disconnect();
            _deviceMap.Remove(device);
        }

        public void InputReportReceived(IDualShockDevice device, IInputReport report)
        {
            switch (device.DeviceType)
            {
                case DualShockDeviceType.DualShock3:

                    var target = _deviceMap[device];
                    target.ResetReport();

                    var ds3Report = (DualShock3InputReport)report;

                    foreach (var axis in _XaxisMap) target.SetAxisValue(axis.Value, ds3Report[axis.Key]);

                    foreach (var axis in _YaxisMap) target.SetAxisValue(axis.Value, ds3Report[axis.Key]);

                    foreach (var axis in _triggerAxisMap) target.SetSliderValue(axis.Value, ds3Report[axis.Key]);

                    foreach (var button in _btnMap.Where(m => ds3Report.EngagedButtons.Contains(m.Key))
                        .Select(m => m.Value)) target.SetButtonState(button, true);

                    if (ds3Report.EngagedButtons.Contains(DualShock3Buttons.DPadUp))
                        target.SetDPadDirection(DualShock4DPadDirection.North);
                    if (ds3Report.EngagedButtons.Contains(DualShock3Buttons.DPadRight))
                        target.SetDPadDirection(DualShock4DPadDirection.East);
                    if (ds3Report.EngagedButtons.Contains(DualShock3Buttons.DPadDown))
                        target.SetDPadDirection(DualShock4DPadDirection.South);
                    if (ds3Report.EngagedButtons.Contains(DualShock3Buttons.DPadLeft))
                        target.SetDPadDirection(DualShock4DPadDirection.West);

                    if (ds3Report.EngagedButtons.Contains(DualShock3Buttons.DPadUp)
                        && ds3Report.EngagedButtons.Contains(DualShock3Buttons.DPadRight))
                        target.SetDPadDirection(DualShock4DPadDirection.Northeast);
                    if (ds3Report.EngagedButtons.Contains(DualShock3Buttons.DPadRight)
                        && ds3Report.EngagedButtons.Contains(DualShock3Buttons.DPadDown))
                        target.SetDPadDirection(DualShock4DPadDirection.Southeast);
                    if (ds3Report.EngagedButtons.Contains(DualShock3Buttons.DPadDown)
                        && ds3Report.EngagedButtons.Contains(DualShock3Buttons.DPadLeft))
                        target.SetDPadDirection(DualShock4DPadDirection.Southwest);
                    if (ds3Report.EngagedButtons.Contains(DualShock3Buttons.DPadLeft)
                        && ds3Report.EngagedButtons.Contains(DualShock3Buttons.DPadUp))
                        target.SetDPadDirection(DualShock4DPadDirection.Northwest);

                    target.SubmitReport();

                    break;
            }
        }
    }
#endif

#if X360

    internal class ಠ_ಠAttribute : Attribute
    {
    }

    [ExportMetadata("Name", "ViGEm Xbox 360 Sink")]
    [Export(typeof(ISinkPlugin))]
    public class ViGEmSink : ISinkPlugin
    {
        private readonly Dictionary<DualShock3Buttons, Xbox360Button> _btnMap;
        private readonly ViGEmClient _client;

        private readonly Dictionary<IDualShockDevice, IXbox360Controller> _deviceMap =
            new Dictionary<IDualShockDevice, IXbox360Controller>();

        private readonly Dictionary<DualShock3Axes, Xbox360Slider> _triggerAxisMap;
        private readonly Dictionary<DualShock3Axes, Xbox360Axis> _XaxisMap;
        private readonly Dictionary<DualShock3Axes, Xbox360Axis> _YaxisMap;

        public ViGEmSink()
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

        public void DeviceArrived(IDualShockDevice device)
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
                Log.Information("Connecting ViGEm target {Target}", target);
                target.Connect();
                Log.Information("ViGEm target {Target} connected successfully", target);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to connect target {@Target}: {Exception}", target, ex);
            }
        }

        public void DeviceRemoved(IDualShockDevice device)
        {
            _deviceMap[device].Disconnect();
            _deviceMap.Remove(device);
        }

        public void InputReportReceived(IDualShockDevice device, IInputReport report)
        {
            switch (device.DeviceType)
            {
                case DualShockDeviceType.DualShock3:

                    var target = _deviceMap[device];
                    target.ResetReport();

                    var ds3Report = (DualShock3InputReport) report;

                    foreach (var axis in _XaxisMap) target.SetAxisValue(axis.Value, Scale(ds3Report[axis.Key], false));

                    foreach (var axis in _YaxisMap) target.SetAxisValue(axis.Value, Scale(ds3Report[axis.Key], true));

                    foreach (var axis in _triggerAxisMap) target.SetSliderValue(axis.Value, ds3Report[axis.Key]);

                    foreach (var button in _btnMap.Where(m => ds3Report.EngagedButtons.Contains(m.Key))
                        .Select(m => m.Value)) target.SetButtonState(button, true);

                    target.SubmitReport();

                    break;
            }
        }

        [ಠ_ಠ]
        private static short Scale(byte value, bool invert)
        {
            var intValue = value - 0x80;
            if (intValue == -128) intValue = -127;

            var wtfValue = intValue * 258.00787401574803149606299212599f; // what the fuck?

            return (short) (invert ? -wtfValue : wtfValue);
        }
    }
#endif
}