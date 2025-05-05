/********************************************************************************
* StreamExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.IO;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Helper methods for the <see cref="Stream"/> type.
    /// </summary>
    internal static class StreamExtensions
    {
        /// <summary>
        /// Converts the given <see cref="Stream"/> to a <see cref="byte"/> array.
        /// </summary>
        public static byte[] ToArray(this Stream self)
        {
            byte[] buffer = new byte[self.Length];
            self.Read(buffer, 0, buffer.Length);
            return buffer;
        }
    }
}
