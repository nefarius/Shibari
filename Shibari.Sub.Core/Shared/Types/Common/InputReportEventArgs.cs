using System;

namespace Shibari.Sub.Core.Shared.Types.Common
{
    public class InputReportEventArgs : EventArgs
    {
        public InputReportEventArgs(IInputReport report)
        {
            Report = report;
        }

        public IInputReport Report { get; }
    }
}