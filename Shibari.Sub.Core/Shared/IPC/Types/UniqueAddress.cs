using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Shibari.Sub.Core.Util;

namespace Shibari.Sub.Core.Shared.IPC.Types
{
    public class UniqueAddress
    {
        public byte[] AddressBytes { get; }

        [JsonConstructor]
        public UniqueAddress(byte[] addressBytes)
        {
            AddressBytes = addressBytes;
        }

        public UniqueAddress(PhysicalAddress address)
        {
            AddressBytes = address.GetAddressBytes();
        }

        public UniqueAddress(string address)
        {
            var validate = new Regex(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$");
            var replace = new Regex(@"[:\- ]");

            if (!validate.IsMatch(address))
                throw new ArgumentException($"{address} isn't a valid MAC address.");

            AddressBytes = PhysicalAddress.Parse(replace.Replace(address, "")).GetAddressBytes();
        }

        public override string ToString()
        {
            return new PhysicalAddress(AddressBytes).AsFriendlyName();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UniqueAddress)obj);
        }

        protected bool Equals(UniqueAddress other)
        {
            return AddressBytes.SequenceEqual(other.AddressBytes);
        }

        public override int GetHashCode()
        {
            return AddressBytes.GetHashCode();
        }

        public static bool operator ==(UniqueAddress left, UniqueAddress right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(UniqueAddress left, UniqueAddress right)
        {
            return !Equals(left, right);
        }
    }
}