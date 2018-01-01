using System;
using System.Net.NetworkInformation;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Util;
using Shibari.Sub.Source.AirBender.Core.Host;

namespace Shibari.Sub.Source.AirBender.Core.Children
{
    /// <summary>
    ///     Represents a managed wrapper for a Bluetooth host child device.
    /// </summary>
    internal abstract class AirBenderChildDevice : DualShockDevice
    {
        /// <summary>
        ///     Creates a new child device.
        /// </summary>
        /// <param name="host">The host this child is connected to.</param>
        /// <param name="client">The client MAC address identifying this child.</param>
        /// <param name="index">The index this child is registered on the host device under.</param>
        protected AirBenderChildDevice(AirBenderHost host, PhysicalAddress client, int index) : base(
            DualShockConnectionType.Bluetooth, host.DeviceHandle, index)
        {
            HostDevice = host;
            HostAddress = host.HostAddress;
            ClientAddress = client;
        }

        protected AirBenderHost HostDevice { get; }

        public override void PairTo(PhysicalAddress host)
        {
            throw new NotSupportedException("You can not change the host address while connected via Bluetooth.");
        }

        public override string ToString()
        {
            return $"{DeviceType} ({ClientAddress.AsFriendlyName()})";
        }
    }
}