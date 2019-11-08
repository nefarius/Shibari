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

    //TODO: Clean up and test with extra controllers

    [ExportMetadata("Name", "ViGEm Dual Navigation to Xbox 360 Sink")]
    [Export(typeof(ISinkPlugin))]
    public class ViGEmNavMergeX360Sink : ISinkPlugin
    {
        private readonly Dictionary<DualShock3Buttons, Xbox360Button> _btnMap0;
        private readonly Dictionary<DualShock3Buttons, Xbox360Button> _btnMap1;
        private readonly ViGEmClient _client;
        private IXbox360Controller _target;
        private int _deviceCount;

        private DualShock3InputReport _Nav0Report;
        private DualShock3InputReport _Nav1Report;

        public ViGEmNavMergeX360Sink()
        {
            _deviceCount = 0;
            _Nav0Report = null;
            _Nav1Report = null;

            _btnMap0 = new Dictionary<DualShock3Buttons, Xbox360Button>
            {
                {DualShock3Buttons.LeftThumb, Xbox360Button.LeftThumb},
                {DualShock3Buttons.LeftShoulder, Xbox360Button.LeftShoulder},
                {DualShock3Buttons.Circle, Xbox360Button.Start},
                {DualShock3Buttons.Cross, Xbox360Button.Back},
                {DualShock3Buttons.DPadUp, Xbox360Button.Up},
                {DualShock3Buttons.DPadDown, Xbox360Button.Down},
                {DualShock3Buttons.DPadLeft, Xbox360Button.Left},
                {DualShock3Buttons.DPadRight, Xbox360Button.Right},
                {DualShock3Buttons.Ps, Xbox360Button.Guide}
            };

            _btnMap1 = new Dictionary<DualShock3Buttons, Xbox360Button>
            {
                {DualShock3Buttons.LeftThumb, Xbox360Button.RightThumb},
                {DualShock3Buttons.LeftShoulder, Xbox360Button.RightShoulder},
                {DualShock3Buttons.Circle, Xbox360Button.B},
                {DualShock3Buttons.Cross, Xbox360Button.A},
                {DualShock3Buttons.DPadUp, Xbox360Button.Y},
                {DualShock3Buttons.DPadDown, Xbox360Button.A},
                {DualShock3Buttons.DPadLeft, Xbox360Button.X},
                {DualShock3Buttons.DPadRight, Xbox360Button.B},
                {DualShock3Buttons.Ps, Xbox360Button.Guide}
            };

            _client = new ViGEmClient();
        }

        public event RumbleRequestReceivedEventHandler RumbleRequestReceived;

        [HandleProcessCorruptedStateExceptions]
        public void DeviceArrived(IDualShockDevice device)
        {
            Log.Information("ViGEmNavMergeX360: Device with index {Index} attached", device.DeviceIndex);
            // Only create virtual 360 controller if one hasn't been created yet
            if (_target == null)
            {
                _target = _client.CreateXbox360Controller();

                _target.AutoSubmitReport = false;

                _target.FeedbackReceived += (sender, args) =>
                {
                    //TODO: Check if needed
                };

                try
                {
                    Log.Information("ViGEmNavMergeX360: Connecting ViGEm target {Target}", _target);
                    _target.Connect();
                    Log.Information("ViGEmNavMergeX360: ViGEm target {Target} connected successfully", _target);
                }
                catch (Exception ex)
                {
                    Log.Error("ViGEmNavMergeX360: Failed to connect target {@Target}: {Exception}", _target, ex);
                }
            }

            _deviceCount++;
        }

        [HandleProcessCorruptedStateExceptions]
        public void DeviceRemoved(IDualShockDevice device)
        {
            Log.Information("ViGEmNavMergeX360: Device with index {Index} detached", device.DeviceIndex);
            // Only remove the virtual 360 controller if we only had one controller left connected
            if (_deviceCount == 1)
            {
                _target.Disconnect();
                _target = null;
            }

            _deviceCount--;

            if (device.DeviceIndex == 0) _Nav0Report = null;
            if (device.DeviceIndex == 1) _Nav1Report = null;
        }

        [HandleProcessCorruptedStateExceptions]
        public void InputReportReceived(IDualShockDevice device, IInputReport report)
        {
            _target.ResetReport(); //This may be able to be optimized, look into later...

            // Convert report to DS3 format and store latest report for this device
            var ds3Report = (DualShock3InputReport)report;

            if (device.DeviceIndex == 0) _Nav0Report = ds3Report;
            if (device.DeviceIndex == 1) _Nav1Report = ds3Report;

            // Only combine reports and submit if we've seen input from each controller at least once
            if (_Nav0Report != null && _Nav1Report != null)
            {
                // Map buttons from Navigation #1 into input report
                _target.SetAxisValue(Xbox360Axis.LeftThumbX, Scale(_Nav0Report[DualShock3Axes.LeftThumbX], false));
                _target.SetAxisValue(Xbox360Axis.LeftThumbY, Scale(_Nav0Report[DualShock3Axes.LeftThumbY], true));
                _target.SetAxisValue(Xbox360Axis.RightThumbX, Scale(_Nav1Report[DualShock3Axes.LeftThumbX], false));
                _target.SetAxisValue(Xbox360Axis.RightThumbY, Scale(_Nav1Report[DualShock3Axes.LeftThumbY], true));

                _target.SetSliderValue(Xbox360Slider.LeftTrigger, _Nav0Report[DualShock3Axes.LeftTrigger]);
                _target.SetSliderValue(Xbox360Slider.RightTrigger, _Nav1Report[DualShock3Axes.LeftTrigger]);

                foreach (var button in _btnMap0.Where(m => _Nav0Report.EngagedButtons.Contains(m.Key))
                    .Select(m => m.Value)) _target.SetButtonState(button, true);

                foreach (var button in _btnMap1.Where(m => _Nav1Report.EngagedButtons.Contains(m.Key))
                    .Select(m => m.Value)) _target.SetButtonState(button, true);

                _target.SubmitReport();
            }
        }

        [ಠ_ಠ]
        private static short Scale(byte value, bool invert)
        {
            var intValue = value - 0x80;
            if (intValue == -128) intValue = -127;

            var wtfValue = intValue * 258.00787401574803149606299212599f; // what the fuck? (TODO: Find the fractional value that gives this again)

            return (short)(invert ? -wtfValue : wtfValue);
        }
    }
}