using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Shared.Types.Common.Sinks;
using Shibari.Sub.Core.Shared.Types.DualShock3;


namespace Shibari.Sub.Sink.ViGEm.Core
{

    internal class ಠ_ಠAttribute : Attribute
    { }

    [ExportMetadata("Name", "ViGEm Xbox 360 Sink")]
    [Export(typeof(ISinkPlugin))]
    public class ViGEmSink : ISinkPlugin
    {
        private readonly Dictionary<DualShock3Buttons, Xbox360Buttons> _btnMap;
        private readonly Dictionary<DualShock3Axes, Xbox360Axes> _XaxisMap;
        private readonly Dictionary<DualShock3Axes, Xbox360Axes> _YaxisMap;
        private readonly Dictionary<DualShock3Axes, Xbox360Axes> _triggerAxisMap;
        private readonly ViGEmClient _client;

        private readonly Dictionary<IDualShockDevice, Xbox360Controller> _deviceMap =
            new Dictionary<IDualShockDevice, Xbox360Controller>();

        public ViGEmSink()
        {
            _btnMap = new Dictionary<DualShock3Buttons, Xbox360Buttons>
            {
                {DualShock3Buttons.Select, Xbox360Buttons.Back},
                {DualShock3Buttons.LeftThumb, Xbox360Buttons.LeftThumb},
                {DualShock3Buttons.RightThumb, Xbox360Buttons.RightThumb},
                {DualShock3Buttons.Start, Xbox360Buttons.Start},
                {DualShock3Buttons.LeftShoulder, Xbox360Buttons.LeftShoulder},
                {DualShock3Buttons.RightShoulder, Xbox360Buttons.RightShoulder},
                {DualShock3Buttons.Triangle, Xbox360Buttons.Y},
                {DualShock3Buttons.Circle, Xbox360Buttons.B},
                {DualShock3Buttons.Cross, Xbox360Buttons.A},
                {DualShock3Buttons.Square, Xbox360Buttons.X},
                {DualShock3Buttons.DPadUp, Xbox360Buttons.Up},
                {DualShock3Buttons.DPadDown, Xbox360Buttons.Down},
                {DualShock3Buttons.DPadLeft, Xbox360Buttons.Left},
                {DualShock3Buttons.DPadRight, Xbox360Buttons.Right},
                {DualShock3Buttons.Ps, Xbox360Buttons.Guide}
            };

            _XaxisMap = new Dictionary<DualShock3Axes, Xbox360Axes>
            {
                {DualShock3Axes.LeftThumbX, Xbox360Axes.LeftThumbX},
                {DualShock3Axes.RightThumbX, Xbox360Axes.RightThumbX}
            };

            _YaxisMap = new Dictionary<DualShock3Axes, Xbox360Axes>
            {
                {DualShock3Axes.LeftThumbY, Xbox360Axes.LeftThumbY},
                {DualShock3Axes.RightThumbY, Xbox360Axes.RightThumbY},
            };

            _triggerAxisMap = new Dictionary<DualShock3Axes, Xbox360Axes>
            {
                {DualShock3Axes.LeftTrigger, Xbox360Axes.LeftTrigger},
                {DualShock3Axes.RightTrigger, Xbox360Axes.RightTrigger},
            };

            _client = new ViGEmClient();
        }

        public event RumbleRequestReceivedEventHandler RumbleRequestReceived;

        public void DeviceArrived(IDualShockDevice device)
        {
            var target = new Xbox360Controller(_client);

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
            _deviceMap[device].Dispose();
            _deviceMap.Remove(device);
        }

        public void InputReportReceived(IDualShockDevice device, IInputReport report)
        {
            switch (device.DeviceType)
            {
                case DualShockDeviceType.DualShock3:

                    var target = _deviceMap[device];

                    var ds3Report = (DualShock3InputReport)report;
                    var xb360Report = new Xbox360Report();

                    foreach (var axis in _XaxisMap)
                    {
                        xb360Report.SetAxis(axis.Value, Scale(ds3Report[axis.Key], false));
                    }

                    foreach (var axis in _YaxisMap)
                    {
                        xb360Report.SetAxis(axis.Value, Scale(ds3Report[axis.Key], true));
                    }

                    foreach (var axis in _triggerAxisMap)
                    {
                        xb360Report.SetAxis(axis.Value, ds3Report[axis.Key]);
                    }

                    xb360Report.SetButtons(_btnMap.Where(m => ds3Report.EngagedButtons.Contains(m.Key))
                        .Select(m => m.Value).ToArray());

                    target.SendReport(xb360Report);

                    break;
            }
        }

        [ಠ_ಠ]
        private static short Scale(byte value, bool invert)
        {
            int intValue = (value - 0x80);
            if (intValue == -128) intValue = -127;

            var wtfValue = intValue * 258.00787401574803149606299212599f; // what the fuck?

            return (short)(invert ? -wtfValue : wtfValue);
        }
    }

}