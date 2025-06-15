/********************************************************************************
* ExceptionExtensions.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Helper methods for the <see cref="Exception"/> class.
    /// </summary>
    internal static class ExceptionExtensions
    {
        /// <summary>
        /// Returns true if the provided exception was thrown from user code, false otherwise.
        /// </summary>
        /// <remarks>The method must be called within a catch block.</remarks>
        public static bool IsUser(this Exception src) => new StackTrace(src)
            .GetFrames()
            .First()
            .GetMethod()
            .DeclaringType
            .FullName
            .StartsWith(typeof(IInterceptor).Namespace);
    }
}
