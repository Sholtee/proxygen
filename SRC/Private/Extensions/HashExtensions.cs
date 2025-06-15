/********************************************************************************
* HashExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Security.Cryptography;
using System.Text;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Hashing helpers.
    /// </summary>
    internal static class HashExtensions
    {
        /// <summary>
        /// Stringifies the given hash using the specified format string.
        /// </summary>
        public static string Stringify(this HashAlgorithm self, string format)
        {
            self.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            StringBuilder sb = new();

            for (int i = 0; i < self.Hash.Length; i++)
                sb.Append(self.Hash[i].ToString(format, null));

            return sb.ToString();
        }

        /// <summary>
        /// Updates the given transformation with provided string value.
        /// </summary>
        public static void Update(this ICryptoTransform transform, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            transform.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
        }
    }
}
