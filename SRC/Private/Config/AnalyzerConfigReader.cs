/********************************************************************************
* AnalyzerConfigReader.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class AnalyzerConfigReader : IConfigReader
    {
        public AnalyzerConfigOptions ConfigOptions { get; }

        public string BasePath => ReadValueInternal("MSBuildThisFileDirectory")!;

        public AnalyzerConfigReader(in GeneratorExecutionContext context) =>
            ConfigOptions = context.AnalyzerConfigOptions.GlobalOptions;

        private string? ReadValueInternal(string name) => ConfigOptions.TryGetValue($"build_property.{name}", out string? value)
            ? value
            : null;

        public string? ReadValue(string name) => ReadValueInternal($"ProxyGen_{name}");
    }
}
