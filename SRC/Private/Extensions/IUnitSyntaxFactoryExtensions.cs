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

            return new SourceCode(src.GetHint(), src.Unit!.NormalizeWhitespace(eol: Environment.NewLine).ToFullString());
        }

        public static string GetHint(this IUnitSyntaxFactory src) => $"{src.DefinedClasses.Single()}.cs";

        public static bool BuildAndDump(this IUnitSyntaxFactory src, CancellationToken cancellation) 
        {
            if (!src.Build(cancellation))
                return false;

            if (SourceDump is not null)
            {
                string 
                    hint = src.GetHint(),
                    hintRefs = $"{hint}.references",
                    code = src.Unit!.NormalizeWhitespace(eol: Environment.NewLine).ToFullString(),
                    references = string.Join(Environment.NewLine, src.References.Select(@ref => $"{@ref.Name}: {@ref.Location ?? "NULL"}"));

                try
                {
#if NETSTANDARD2_0
                    File.WriteAllText
                    (
                        Path.Combine(SourceDump, hint),
                        code
                    );
                    File.WriteAllText
                    (
                        Path.Combine(SourceDump, hintRefs),
                        references
                    );
#else
                    File.WriteAllTextAsync
                    (
                        Path.Combine(SourceDump, hint),
                        code,
                        cancellation
                    ).Wait();
                    File.WriteAllTextAsync
                    (
                        Path.Combine(SourceDump, hintRefs),
                        references,
                        cancellation
                    ).Wait();
#endif
                }
                catch { }
            }

            return true;
        }
    }
}
