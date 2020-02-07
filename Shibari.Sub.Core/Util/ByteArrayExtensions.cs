using System;

namespace Shibari.Sub.Core.Util
{
    public static class ByteArrayExtensions
    {
        public static string ToHexString(this byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }
    }
}