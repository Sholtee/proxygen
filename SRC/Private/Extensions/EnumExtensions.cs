/********************************************************************************
* EnumExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Helper methods for the <see cref="Enum"/> type.
    /// </summary>
    internal static class EnumExtensions
    {
        /// <summary>
        /// Returns the flags set in the given enum.
        /// </summary>
        public static IEnumerable<TEnum> SetFlags<TEnum>(this TEnum self) where TEnum: struct, Enum
        {
            foreach (TEnum flag in Enum.GetValues(typeof(TEnum)))
                if (!flag.Equals(default(TEnum)) && self.HasFlag(flag))
                    yield return flag;
        }
    }
}
