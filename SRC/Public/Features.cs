/********************************************************************************
* Features.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Specifies the global configuration of this library.
    /// </summary>
    public static class Features
    {
        internal const LanguageVersion LV_MAX_SUPPORTED = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7;

        /// <summary>
        /// The language version being used during compilation.
        /// </summary>
        public static string LanguageVersion { get; } = LV_MAX_SUPPORTED.ToString();
    }
}
