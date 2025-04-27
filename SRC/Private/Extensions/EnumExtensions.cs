/********************************************************************************
* EnumExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    internal static class EnumExtensions
    {
        public static IEnumerable<TEnum> SetFlags<TEnum>(this TEnum self) where TEnum: struct, Enum
        {
            foreach (TEnum flag in Enum.GetValues(typeof(TEnum)))
                if (!flag.Equals(default(TEnum)) && self.HasFlag(flag))
                    yield return flag;
        }
    }
}
