/********************************************************************************
* Diagnostics.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static class Diagnostics
    {
        public delegate Diagnostic DiagnosticFactory(Location location, params object[] args);

        public static DiagnosticFactory PGE00 { get; } = CreateFactory(nameof(PGE00), SGResources.LNG_NOT_SUPPORTED, SGResources.LNG_NOT_SUPPORTED, DiagnosticSeverity.Warning);

        public static DiagnosticFactory PGE01 { get; } = CreateFactory(nameof(PGE01), SGResources.TE_FAILED, SGResources.TE_FAILED_FULL, DiagnosticSeverity.Warning);

        public static DiagnosticFactory PGI00 { get; } = CreateFactory(nameof(PGI00), SGResources.SRC_EXTENDED, SGResources.SRC_EXTENDED_FULL, DiagnosticSeverity.Info);

        private static DiagnosticFactory CreateFactory(string id, string message, string messageFtm, DiagnosticSeverity severity) => (location, args) => Diagnostic.Create
        (
            new DiagnosticDescriptor(id, message, string.Format(SGResources.Culture, messageFtm, args), SGResources.TE, severity, true),
            location
        );
    }
}
