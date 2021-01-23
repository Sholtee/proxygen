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

        public string BasePath => ReadValueInternal("MSBuildProjectDirectory")!;

        public AnalyzerConfigReader(in GeneratorExecutionContext context) =>
            ConfigOptions = context.AnalyzerConfigOptions.GlobalOptions;

        //
        // Ugy tunik h a CompilerVisibleProperty (lasd ProxyGen.NET.targets) miatt ha az adott build property neve
        // ismert a TryGetValue() mindenkepp igazzal ter vissza meg akkor is ha a tulajdonsag nincs definialva 
        // sem -p kapcsoloval sem a csproj-ban.
        // Ilyenkor az ertek ures karakterlanc lesz (ami a csproj mappajara mutat, az nekunk nyilvan nem jo).
        //

        private string? ReadValueInternal(string name) => ConfigOptions.TryGetValue($"build_property.{name}", out string? value) && !string.IsNullOrEmpty(value)
            ? value
            : null;

        public string? ReadValue(string name) => ReadValueInternal($"ProxyGen_{name}");
    }
}
