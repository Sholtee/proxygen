/********************************************************************************
* Visibility.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static class Visibility
    {
        public static void Check(MethodBase method, string assemblyName, bool allowProtected = false) 
        {
            AccessModifiers am = method.GetAccessModifiers();

            if (am.HasFlag(AccessModifiers.Internal))
            {
                bool grantedByAttr = method
                    .DeclaringType
                    .Assembly
                    .GetCustomAttributes<InternalsVisibleToAttribute>()
                    .FirstOrDefault(ivt => ivt.AssemblyName == assemblyName) != null;

                if (grantedByAttr) return;

                if (!am.HasFlag(AccessModifiers.Protected) /*protected-internal*/)
                {
                    throw new MemberAccessException(string.Format(Resources.Culture, Resources.IVT_REQUIRED, method.GetFullName(), assemblyName));
                }
            }

            if (am.HasFlag(AccessModifiers.Protected)) 
            {
                if (allowProtected) return;
                throw new MemberAccessException(string.Format(Resources.Culture, Resources.METHOD_NOT_VISIBLE, method.GetFullName()));
            }

            //
            // "Private", "Explicit" mellett mas nem szerepelhet -> nem kell HasFlag()
            //

            if (am == AccessModifiers.Explicit) return; // meg ha cast-olni is kell hozza de lathato

            if (am == AccessModifiers.Private)
                throw new MemberAccessException(string.Format(Resources.Culture, Resources.METHOD_NOT_VISIBLE, method.GetFullName()));

            Debug.Assert(am == AccessModifiers.Public, $"Unknown AccessModifier: {am}");
        }

        public static void Check(PropertyInfo property, string assemblyName, bool checkGet = true, bool checkSet = true, bool allowProtected = false) 
        {
            if (checkGet) 
            {
                MethodInfo get = property.GetMethod;
                Debug.Assert(get != null, "property.GetMethod == NULL");

                Check(get!, assemblyName, allowProtected);
            }

            if (checkSet)
            {
                MethodInfo set = property.SetMethod;
                Debug.Assert(set != null, "property.SetMethod == NULL");

                Check(set!, assemblyName, allowProtected);
            }
        }

        public static void Check(EventInfo @event, string assemblyName, bool checkAdd = true, bool checkRemove = true, bool allowProtected = false) 
        {
            if (checkAdd)
            {
                MethodInfo add = @event.AddMethod;
                Debug.Assert(add != null, "event.AddMethod == NULL");

                Check(add!, assemblyName, allowProtected);
            }

            if (checkRemove)
            {
                MethodInfo remove = @event.RemoveMethod;
                Debug.Assert(remove != null, "event.RemoveMethod == NULL");

                Check(remove!, assemblyName, allowProtected);
            }
        }

        public static void Check(Type type, string assemblyName) // TODO: nem kene megszolitani a forditot hozza (meg akkor se ha csak diagnosztikakat kerunk le)
        {
            //
            // Mivel az "internal" es "protected" kulcsszavak nem leteznek IL szinten ezert reflexioval
            // nem tudnank megallapitani h a tipus lathato e a kodunk szamara szoval a forditora bizzuk
            // a dontest:
            //
            // using t = Namespace.Type;
            //

            (CompilationUnitSyntax Unit, IReadOnlyCollection<MetadataReference> References) context = new VisibilityCheckSyntaxFactory(type).GetContext();

            Debug.WriteLine(context.Unit.NormalizeWhitespace().ToFullString());

            CSharpCompilation compilation = CSharpCompilation.Create
            (
                assemblyName: assemblyName,
                syntaxTrees: new[]
                {
                    CSharpSyntaxTree.Create
                    (
                        root: context.Unit
                    )
                },
                references: context.References,
                options: CompilationOptionsFactory.Create()
            );

            Diagnostic[] diagnostics = compilation
                .GetDeclarationDiagnostics()
                .Where(diag => diag.Severity == DiagnosticSeverity.Error)
                .ToArray();

            if (diagnostics.Length == 0) return;

            //
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0122
            //

            if (diagnostics.Length > 1 || !diagnostics.Single().Id.Equals("CS0122", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception(string.Join(Environment.NewLine, diagnostics.Select(diagnostic => diagnostic.GetMessage())));
            }

            //
            // A fordito nem fogja megmondani h mi a tipus lathatosaga csak azt h lathato e v sem,
            // ezert vmi altalanosabb hibauzenet kell.
            //

            throw new MemberAccessException(string.Format(Resources.Culture, Resources.TYPE_NOT_VISIBLE, type, assemblyName));
        }
    }
}