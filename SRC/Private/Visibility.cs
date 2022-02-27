/********************************************************************************
* Visibility.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
                    ?.IsFriend(assemblyName) is true;

                if (grantedByAttr)
                    return;

                if (!am.HasFlag(AccessModifiers.Protected) /*protected-internal*/)
                {
                    throw new MemberAccessException(string.Format(Resources.Culture, Resources.IVT_REQUIRED, method.Name, assemblyName));
                }
            }

            if (am.HasFlag(AccessModifiers.Protected)) 
            {
                if (allowProtected)
                    return;
                throw new MemberAccessException(string.Format(Resources.Culture, Resources.METHOD_NOT_VISIBLE, method.Name));
            }

            //
            // "Private", "Explicit" mellett mas nem szerepelhet -> nem kell HasFlag()
            //

            if (am is AccessModifiers.Explicit)
                return; // meg ha cast-olni is kell hozza de lathato

            if (am is AccessModifiers.Private)
                throw new MemberAccessException(string.Format(Resources.Culture, Resources.METHOD_NOT_VISIBLE, method.Name));

            Debug.Assert(am is AccessModifiers.Public, $"Unknown AccessModifier: {am}");
        }

        public static void Check(IPropertyInfo property, string assemblyName, bool checkGet = true, bool checkSet = true, bool allowProtected = false) 
        {
            if (checkGet) 
            {
                IMethodInfo? get = property.GetMethod;
                Debug.Assert(get is not null, "property.GetMethod == NULL");

                Check(get!, assemblyName, allowProtected);
            }

            if (checkSet)
            {
                IMethodInfo? set = property.SetMethod;
                Debug.Assert(set is not null, "property.SetMethod == NULL");

                Check(set!, assemblyName, allowProtected);
            }
        }

        public static void Check(IEventInfo @event, string assemblyName, bool checkAdd = true, bool checkRemove = true, bool allowProtected = false) 
        {
            if (checkAdd)
            {
                IMethodInfo? add = @event.AddMethod;
                Debug.Assert(add is not null, "event.AddMethod == NULL");

                Check(add!, assemblyName, allowProtected);
            }

            if (checkRemove)
            {
                IMethodInfo? remove = @event.RemoveMethod;
                Debug.Assert(remove is not null, "event.RemoveMethod == NULL");

                Check(remove!, assemblyName, allowProtected);
            }
        }

        public static void Check(ITypeInfo type, string assemblyName)
        {
            if (type.DeclaringAssembly is null)
                throw new NotSupportedException();

            //
            // Tomb es mutato tipusnal az elem tipusat kell vizsgaljuk
            //

            if (type.ElementType is not null)
            {
                Check(type.ElementType, assemblyName);
                return;
            }

            ReferenceCollector collector = new(includeRuntimeReferences: false);
            collector.AddType(type);

            //
            // Korbedolgozas arra az esetre ha a "type" GeneratorExecutionContext-bol jon es nem
            // teljes ujraforditas van
            //

            if (collector.References.Some(@ref => @ref.Location is null))
                return;

            //
            // Mivel az "internal" es "protected" kulcsszavak nem leteznek IL szinten ezert reflexioval
            // nem tudnank megallapitani h a tipus lathato e a kodunk szamara szoval a forditotol kerjuk
            // el.
            //

            CSharpCompilation comp = CSharpCompilation.Create
            (
                null,
                references: collector
                    .References
                    .Convert(@ref => MetadataReference.CreateFromFile(@ref.Location!))
            );

            switch (type.ToSymbol(comp).DeclaredAccessibility) 
            {
                case Accessibility.Private:
                    throw new MemberAccessException(string.Format(Resources.Culture, Resources.TYPE_NOT_VISIBLE, type));
                case Accessibility.Internal when !type.DeclaringAssembly.IsFriend(assemblyName):
                    throw new MemberAccessException(string.Format(Resources.Culture, Resources.IVT_REQUIRED, type, assemblyName));
                case Accessibility.NotApplicable:
                    throw new InvalidOperationException();
            }
        }
    }
}