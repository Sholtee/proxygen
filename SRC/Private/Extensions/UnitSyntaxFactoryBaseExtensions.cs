/********************************************************************************
* UnitSyntaxFactoryBaseExtensions.cs                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal static class UnitSyntaxFactoryBaseExtensions
    {
        public static SourceCode GetSourceCode(this UnitSyntaxFactoryBase src, in CancellationToken cancellation) => new
        (
            src.GetHint(),
            src.ResolveUnitAndDump(cancellation)
        );

        public static string GetHint(this UnitSyntaxFactoryBase src) => $"{src.ExposedClass}.cs";

        public static CompilationUnitSyntax ResolveUnitAndDump(this UnitSyntaxFactoryBase src, CancellationToken cancellation) 
        {
            CompilationUnitSyntax unit = src.ResolveUnit(null!, cancellation);

            string? sourceDump = WorkingDirectories.Instance.SourceDump;

            if (sourceDump is not null)
            {
                string hint = src.GetHint();

                StreamWriter log;

                Directory.CreateDirectory(sourceDump);

                using (log = File.CreateText(Path.Combine(sourceDump, hint)))
                {
                    log.AutoFlush = true;
                    unit
                        .NormalizeWhitespace(eol: Environment.NewLine)
                        .WriteTo(log); // "WriteTo() has no overload having "cancellation" parameter
                }

                if (src.ReferenceCollector is not null)
                {
                    using (log = File.CreateText(Path.Combine(sourceDump, $"{hint}.references")))
                    {
                        log.AutoFlush = true;
                        log.Write
                        (
                            string.Join
                            (
                                Environment.NewLine,
                                src
                                    .ReferenceCollector
                                    .References
                                    .Select(static @ref => $"{@ref.Name}: {@ref.Location ?? "NULL"}")
                            ),
                            cancellation: cancellation
                        );
                    }
                }
            }

            return unit;
        }
    }
}
