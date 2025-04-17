/********************************************************************************
* UnitSyntaxFactoryBaseExtensions.cs                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
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

            string? sourceDump = src.Context.Config.SourceDump;

            if (sourceDump is not null)
            {
                string hint = src.GetHint();

                Directory.CreateDirectory(sourceDump);

                Log(Path.Combine(sourceDump, hint), unit.NormalizeWhitespace(eol: Environment.NewLine).ToFullString(), cancellation);

                if (src.Context.ReferenceCollector is not null)
                {
                    Log
                    (
                        Path.Combine(sourceDump, $"{hint}.references"),
                        string.Join
                        (
                            Environment.NewLine,
                            src
                                .Context
                                .ReferenceCollector
                                .References
                                .Select(static @ref => $"{@ref.Name}: {@ref.Location ?? "NULL"}")
                        ),
                        cancellation
                    );
                }

                static void Log(string file, string data, CancellationToken cancellation)  // TODO: implement real logging
                {
                    try
                    {
                        using StreamWriter log = File.CreateText(file);
                        log.AutoFlush = true;
                        log.Write(data, cancellation: cancellation);
                    }
                    catch (IOException ex)
                    {
                        Trace.TraceWarning($"File ({file}) could not be dumped: ${ex.Message}");
                    }
                }
            }

            return unit;
        }
    }
}
