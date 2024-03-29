﻿/********************************************************************************
* AnalyzerConfigReader.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.Diagnostics;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class AnalyzerConfigReader : IConfigReader
    {
        public AnalyzerConfigOptions ConfigOptions { get; }

        public string BasePath => ReadValueInternal("MSBuildProjectDirectory")!;

        public AnalyzerConfigReader(AnalyzerConfigOptions configOptions) => ConfigOptions = configOptions;

        //
        // It seems due to the CompilerVisibleProperty (see ProxyGen.NET.targets) if the name of a particular build
        // property is known, the TryGetValue() will return true even if the property is not defined.
        //

        private string? ReadValueInternal(string name) => ConfigOptions.TryGetValue($"build_property.{name}", out string? value) && !string.IsNullOrEmpty(value)
            ? value
            : null;

        public string? ReadValue(string name) => ReadValueInternal($"ProxyGen_{name}");
    }
}
