using System;
using System.Collections.Generic;
using System.Linq;

namespace Shibari.Sub.Core.Util
{
    public static class ByteArrayExtensions
    {
        public static string ToHexString(this byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }

        public static int IndexOf(this IEnumerable<byte> arrayToSearchThrough, byte[] patternToFind)
        {
            return IndexOf(arrayToSearchThrough.ToArray(), patternToFind);
        }

        public static int IndexOf(this byte[] arrayToSearchThrough, byte[] patternToFind)
        {
            if (patternToFind.Length > arrayToSearchThrough.Length)
                return -1;

            for (var i = 0; i < arrayToSearchThrough.Length - patternToFind.Length; i++)
            {
                var found = !patternToFind.Where((t, j) => arrayToSearchThrough[i + j] != t).Any();

                if (found) return i;
            }

            return -1;
        }
    }
}