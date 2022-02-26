/********************************************************************************
* IUnitSyntaxFactoryExtensions.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal static class IUnitSyntaxFactoryExtensions
    {
        public static SourceCode GetSourceCode(this IUnitSyntaxFactory src, CancellationToken cancellation) 
        {
            src.BuildAndDump(cancellation);

            return new SourceCode(src.GetHint(), src.Unit!);
        }

        public static string GetHint(this IUnitSyntaxFactory src) => $"{src.DefinedClasses.Single()}.cs";

        public static bool BuildAndDump(this IUnitSyntaxFactory src, CancellationToken cancellation) 
        {
            if (!src.Build(cancellation))
                return false;

            string? sourceDump = WorkingDirectories.Instance.SourceDump;

            if (sourceDump is not null)
            {
                string hint = src.GetHint();

                try
                {
                    StreamWriter log;

                    Directory.CreateDirectory(sourceDump);

                    using (log = File.CreateText(Path.Combine(sourceDump, hint)))
                    {
                        log.AutoFlush = true;
                        src
                            .Unit!
                            .NormalizeWhitespace(eol: Environment.NewLine)
                            .WriteTo(log); // nincs overload ami tamogatna a megszakitast
                    }

                    using (log = File.CreateText(Path.Combine(sourceDump, $"{hint}.references")))
                    {
                        log.AutoFlush = true;
                        log.Write
                        (
                            string.Join
                            (
                                Environment.NewLine,
                                src
                                    .References
                                    .Convert(@ref => $"{@ref.Name}: {@ref.Location ?? "NULL"}")
                            ),
                            cancellation: cancellation
                        );
                    }
                }
                #pragma warning disable CA1031 // This method should not throw
                catch {}
                #pragma warning restore CA1031
            }

            return true;
        }
    }
}
