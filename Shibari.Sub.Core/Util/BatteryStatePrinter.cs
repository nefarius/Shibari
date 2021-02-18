using System;
using Shibari.Sub.Core.Shared.Types.Common;

namespace Shibari.Sub.Core.Util
{
    public class BatteryStatePrinter
    {
        private static bool strobe = true; // Used for strobing the output to show fully charged
        private static int bar_counter = 0; // Used for counting up the bars to show charging
        private static readonly String[] bars = {
                "    ",
                "■   ",
                "■■  ",
                "■■■ ",
                "■■■■",
                "????"};

        public static void printToConsole(DualShockBatterStates BatteryState)
        {
            Console.Write("[");
            switch (BatteryState)
            {
                case DualShockBatterStates.Full:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(bars[4]);
                    break;
                case DualShockBatterStates.High:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(bars[3]);
                    break;
                case DualShockBatterStates.Medium:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(bars[2]);
                    break;
                case DualShockBatterStates.Low:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(bars[1]);
                    break;
                case DualShockBatterStates.Dying:
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (strobe)
                        Console.Write(bars[1]);
                    else
                        Console.Write(bars[0]);
                    strobe = !strobe;
                    break;
                case DualShockBatterStates.Charging:
                    switch (bar_counter)
                    {
                        case 1:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                        case 2:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        case 3:
                            Console.ForegroundColor = ConsoleColor.Green;
                            break;
                        case 4:
                            Console.ForegroundColor = ConsoleColor.Green;
                            break;
                    }
                    Console.Write(bars[bar_counter]);
                    if (++bar_counter > 4) bar_counter = 0;
                    break;
                case DualShockBatterStates.Charged:
                    Console.ForegroundColor = ConsoleColor.Green;
                    if (strobe)
                        Console.Write(bars[4]);
                    else
                        Console.Write(bars[0]);
                    strobe = !strobe;
                    break;
                default:
                    Console.Write(bars[5]);
                    break;
            }

            Console.ResetColor();
            Console.Write("]");
        }
    }
}
