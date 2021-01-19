/********************************************************************************
* IUnitSyntaxFactoryExtensions.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using static WorkingDirectories;

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

            if (SourceDump is not null)
            {
                string hint = src.GetHint();

                try
                {
                    StreamWriter log;

                    Directory.CreateDirectory(SourceDump);

                    using (log = File.CreateText(Path.Combine(SourceDump, hint)))
                    {
                        log.AutoFlush = true;
                        src
                            .Unit!
                            .NormalizeWhitespace(eol: Environment.NewLine)
                            .WriteTo(log); // nincs overload ami tamogatna a megszakitast
                    }

                    using (log = File.CreateText(Path.Combine(SourceDump, $"{hint}.references")))
                    {
                        log.AutoFlush = true;
                        log.Write
                        (
                            string.Join
                            (
                                Environment.NewLine,
                                src
                                    .References
                                    .Select(@ref => $"{@ref.Name}: {@ref.Location ?? "NULL"}")
                            ),
                            cancellation: cancellation
                        );
                    }
                }
                catch {}
            }

            return true;
        }
    }
}
