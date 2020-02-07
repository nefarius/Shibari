using System;
using System.Collections.Generic;
using System.Linq;
using Shibari.Sub.Core.Shared.Exceptions;
using Shibari.Sub.Core.Shared.Types.Common;

namespace Shibari.Sub.Core.Shared.Types.DualShock3
{
    public class DualShock3InputReport : IInputReport
    {
        private DualShock3InputReport(byte[] buffer)
        {
            System.Buffer.BlockCopy(buffer, 0, Buffer, 0, Buffer.Length);
        }

        public IEnumerable<DualShock3Buttons> EngagedButtons
        {
            get
            {
                var buttons =
                    (uint) ((Buffer[2] << 0) | (Buffer[3] << 8) | (Buffer[4] << 16) | (Buffer[5] << 24));

                return Enum.GetValues(typeof(DualShock3Buttons)).Cast<DualShock3Buttons>()
                    .Where(button => (buttons & (uint) button) == (uint) button);
            }
        }

        public DualShockBatterStates BatteryState => (DualShockBatterStates) Buffer[30];

        public byte this[DualShock3Axes axis]
        {
            get
            {
                switch (axis)
                {
                    case DualShock3Axes.LeftThumbX:
                        return Buffer[6];
                    case DualShock3Axes.LeftThumbY:
                        return Buffer[7];
                    case DualShock3Axes.RightThumbX:
                        return Buffer[8];
                    case DualShock3Axes.RightThumbY:
                        return Buffer[9];
                    case DualShock3Axes.DPadUp:
                        return Buffer[14];
                    case DualShock3Axes.DPadRight:
                        return Buffer[15];
                    case DualShock3Axes.DPadDown:
                        return Buffer[16];
                    case DualShock3Axes.DPadLeft:
                        return Buffer[17];
                    case DualShock3Axes.LeftTrigger:
                        return Buffer[18];
                    case DualShock3Axes.RightTrigger:
                        return Buffer[19];
                    case DualShock3Axes.LeftShoulder:
                        return Buffer[20];
                    case DualShock3Axes.RightShoulder:
                        return Buffer[21];
                    case DualShock3Axes.Triangle:
                        return Buffer[22];
                    case DualShock3Axes.Circle:
                        return Buffer[23];
                    case DualShock3Axes.Cross:
                        return Buffer[24];
                    case DualShock3Axes.Square:
                        return Buffer[25];
                }

                throw new InvalidAxisException("The specified axis does not exist");
            }
        }

        public byte[] Buffer { get; } = new byte[49];

        public static DualShock3InputReport FromBuffer(byte[] buffer)
        {
            if (buffer.Length < 49)
                throw new ArgumentOutOfRangeException("buffer", buffer.Length, "Input report too small.");

            if (buffer[0] != 0x01)
                return null;

            return new DualShock3InputReport(buffer);
        }
    }
}