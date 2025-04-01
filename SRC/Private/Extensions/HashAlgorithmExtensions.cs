/********************************************************************************
* HashAlgorithmExtensions.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Security.Cryptography;
using System.Text;

namespace Solti.Utils.Proxy.Internals
{
    internal static class HashAlgorithmExtensions
    {
        public static string ToString(this HashAlgorithm self, string format)
        {
            self.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            StringBuilder sb = new();

            for (int i = 0; i < self.Hash.Length; i++)
                sb.Append(self.Hash[i].ToString(format, null));

            return sb.ToString();
        }
    }
}
