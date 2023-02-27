/********************************************************************************
* StreamExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.IO;

namespace Solti.Utils.Proxy.Internals
{
    internal static class StreamExtensions
    {
        public static byte[] ToArray(this Stream self)
        {
            byte[] buffer = new byte[self.Length];
            self.Read(buffer, 0, buffer.Length);
            return buffer;
        }
    }
}
