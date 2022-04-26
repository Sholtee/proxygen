/********************************************************************************
* Visibility.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using static System.Diagnostics.Debug;

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

            Assert(am is AccessModifiers.Public, $"Unknown AccessModifier: {am}");
        }

        public static void Check(IPropertyInfo property, string assemblyName, bool checkGet = true, bool checkSet = true, bool allowProtected = false) 
        {
            if (checkGet) 
            {
                IMethodInfo? get = property.GetMethod;
                Assert(get is not null, "property.GetMethod == NULL");

                Check(get!, assemblyName, allowProtected);
            }

            if (checkSet)
            {
                IMethodInfo? set = property.SetMethod;
                Assert(set is not null, "property.SetMethod == NULL");

                Check(set!, assemblyName, allowProtected);
            }
        }

        public static void Check(IEventInfo @event, string assemblyName, bool checkAdd = true, bool checkRemove = true, bool allowProtected = false) 
        {
            if (checkAdd)
            {
                IMethodInfo? add = @event.AddMethod;
                Assert(add is not null, "event.AddMethod == NULL");

                Check(add!, assemblyName, allowProtected);
            }

            if (checkRemove)
            {
                IMethodInfo? remove = @event.RemoveMethod;
                Assert(remove is not null, "event.RemoveMethod == NULL");

                Check(remove!, assemblyName, allowProtected);
            }
        }

        public static void Check(ITypeInfo type, string assemblyName)
        {
            if (type.IsGenericParameter)
                return;

            //
            // Tomb es mutato tipusnal az elem tipusat kell vizsgaljuk
            //

            if (type.ElementType is not null)
            {
                Check(type.ElementType, assemblyName);
                return;
            }

            //
            // Generikusnal a generikus parametereket
            //

            if (type is IGenericTypeInfo genericType)
            {
                foreach (ITypeInfo ga in genericType.GenericArguments)
                {
                    Check(ga, assemblyName);
                }

                if (!genericType.IsGenericDefinition)
                    Check(genericType.GenericDefinition, assemblyName);

                return;
            }

            if (type.DeclaringAssembly is null)
                throw new NotSupportedException();

            switch (type.AccessModifiers) 
            {
                case AccessModifiers.Private:
                    throw new MemberAccessException(string.Format(Resources.Culture, Resources.TYPE_NOT_VISIBLE, type));
                case AccessModifiers.Internal when !type.DeclaringAssembly.IsFriend(assemblyName):
                    throw new MemberAccessException(string.Format(Resources.Culture, Resources.IVT_REQUIRED, type, assemblyName));
                default:
                    Assert(type.AccessModifiers is not AccessModifiers.Unknown, "Unknown access modifier");
                    break;
            }
        }
    }
}