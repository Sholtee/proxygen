/********************************************************************************
* Visibility.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static class Visibility
    {
        public static void Check(IMethodInfo method, string assemblyName, bool allowProtected = false) 
        {
            AccessModifiers am = method.AccessModifiers;

            if (am.HasFlag(AccessModifiers.Internal))
            {
                bool grantedByAttr = method
                    .DeclaringType
                    .DeclaringAssembly
                    .IsFriend(assemblyName);

                if (grantedByAttr) return;

                if (!am.HasFlag(AccessModifiers.Protected) /*protected-internal*/)
                {
                    throw new MemberAccessException(string.Format(Resources.Culture, Resources.IVT_REQUIRED, method.Name, assemblyName));
                }
            }

            if (am.HasFlag(AccessModifiers.Protected)) 
            {
                if (allowProtected) return;
                throw new MemberAccessException(string.Format(Resources.Culture, Resources.METHOD_NOT_VISIBLE, method.Name));
            }

            //
            // "Private", "Explicit" mellett mas nem szerepelhet -> nem kell HasFlag()
            //

            if (am == AccessModifiers.Explicit) return; // meg ha cast-olni is kell hozza de lathato

            if (am == AccessModifiers.Private)
                throw new MemberAccessException(string.Format(Resources.Culture, Resources.METHOD_NOT_VISIBLE, method.Name));

            Debug.Assert(am == AccessModifiers.Public, $"Unknown AccessModifier: {am}");
        }

        public static void Check(IPropertyInfo property, string assemblyName, bool checkGet = true, bool checkSet = true, bool allowProtected = false) 
        {
            if (checkGet) 
            {
                IMethodInfo? get = property.GetMethod;
                Debug.Assert(get != null, "property.GetMethod == NULL");

                Check(get!, assemblyName, allowProtected);
            }

            if (checkSet)
            {
                IMethodInfo? set = property.SetMethod;
                Debug.Assert(set != null, "property.SetMethod == NULL");

                Check(set!, assemblyName, allowProtected);
            }
        }

        public static void Check(IEventInfo @event, string assemblyName, bool checkAdd = true, bool checkRemove = true, bool allowProtected = false) 
        {
            if (checkAdd)
            {
                IMethodInfo? add = @event.AddMethod;
                Debug.Assert(add != null, "event.AddMethod == NULL");

                Check(add!, assemblyName, allowProtected);
            }

            if (checkRemove)
            {
                IMethodInfo? remove = @event.RemoveMethod;
                Debug.Assert(remove != null, "event.RemoveMethod == NULL");

                Check(remove!, assemblyName, allowProtected);
            }
        }

        public static void Check(ITypeInfo type, string assemblyName) // FIXME: nyilt generikusokra nem mukodik (igaz egyelore nem is kell)
        {
            //
            // Mivel az "internal" es "protected" kulcsszavak nem leteznek IL szinten ezert reflexioval
            // nem tudnank megallapitani h a tipus lathato e a kodunk szamara szoval a forditora bizzuk
            // a dontest:
            //
            // using t = Namespace.Type;
            //

            (CompilationUnitSyntax Unit, IReadOnlyCollection<MetadataReference> References) = new VisibilityCheckSyntaxFactory(type).GetContext();

            Debug.WriteLine(Unit.NormalizeWhitespace().ToFullString());

            CSharpCompilation compilation = CSharpCompilation.Create
            (
                assemblyName: assemblyName,
                syntaxTrees: new[]
                {
                    CSharpSyntaxTree.Create
                    (
                        root: Unit
                    )
                },
                references: References,
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