/********************************************************************************
* AnalyzerConfig.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.Diagnostics;
using System;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Configuration to be used when the library is loaded as a source generator.
    /// </summary>
    internal sealed class AnalyzerConfig(AnalyzerConfigOptions configOptions) : ConfigBase, ISourceGeneratorConfiguration
    {
        //
        // It seems due to the CompilerVisibleProperty (see ProxyGen.NET.targets) if the name of a particular build
        // property is known, the TryGetValue() will return true even if the property is not defined.
        //
        private string? ReadValueInternal(string name) => configOptions.TryGetValue($"build_property.{name}", out string? value) && !string.IsNullOrEmpty(value)
            ? value
            : null;

        /// <inheritdoc/>
        public bool DebugGenerator => ReadValue(nameof(DebugGenerator))?
            .Equals(true.ToString(), StringComparison.OrdinalIgnoreCase) is true;

        /// <inheritdoc/>
        protected override string BasePath => ReadValueInternal("MSBuildProjectDirectory")!;

        /// <inheritdoc/>
        protected override string? ReadValue(string name) => ReadValueInternal($"ProxyGen_{name}");
    }
}
