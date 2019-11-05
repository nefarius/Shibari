using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.ExceptionServices;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Shared.Types.Common.Sinks;
using Shibari.Sub.Core.Shared.Types.DualShock3;

namespace Shibari.Sub.Sink.ViGEm.DS4.Core
{
    [ExportMetadata("Name", "ViGEm DualShock 4 Sink")]
    [Export(typeof(ISinkPlugin))]
    public class ViGEmSinkDS4 : ISinkPlugin
    {
        private readonly Dictionary<DualShock3Buttons, DualShock4Button> _btnMap;
        private readonly ViGEmClient _client;

        private readonly Dictionary<IDualShockDevice, IDualShock4Controller> _deviceMap =
            new Dictionary<IDualShockDevice, IDualShock4Controller>();

        private readonly Dictionary<DualShock3Axes, DualShock4Slider> _triggerAxisMap;
        private readonly Dictionary<DualShock3Axes, DualShock4Axis> _XaxisMap;
        private readonly Dictionary<DualShock3Axes, DualShock4Axis> _YaxisMap;

        public ViGEmSinkDS4()
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

        [HandleProcessCorruptedStateExceptions]
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

        [HandleProcessCorruptedStateExceptions]
        public void DeviceRemoved(IDualShockDevice device)
        {
            Log.Information("Device {Device} got removed", device);

            _deviceMap[device].Disconnect();
            _deviceMap.Remove(device);
        }

        [HandleProcessCorruptedStateExceptions]
        public void InputReportReceived(IDualShockDevice device, IInputReport report)
        {
            switch (device.DeviceType)
            {
                case DualShockDeviceType.DualShock3:

                    var target = _deviceMap[device];
                    target.ResetReport();

                    var ds3Report = (DualShock3InputReport) report;

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
}