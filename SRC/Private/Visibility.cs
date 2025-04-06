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
            // Here the visibility can be either "Private" or "Explicit" -> HasFlag() not required
            //

            if (am is AccessModifiers.Explicit)
                return; // The method is visible after a type-cast

            if (am is AccessModifiers.Private)
                throw new MemberAccessException(string.Format(Resources.Culture, Resources.METHOD_NOT_VISIBLE, method.Name));

            Assert(am is AccessModifiers.Public, $"Unknown AccessModifier: {am}");
        }

        public static void Check(IPropertyInfo property, string assemblyName, bool allowProtected = false) 
        {
            if (property.GetMethod is not null) 
                Check(property.GetMethod, assemblyName, allowProtected);

            if (property.SetMethod is not null)
                Check(property.SetMethod, assemblyName, allowProtected);
        }

        public static void Check(IEventInfo @event, string assemblyName, bool allowProtected = false) 
        {
            Check(@event.AddMethod, assemblyName, allowProtected);

            Check(@event.RemoveMethod, assemblyName, allowProtected);
        }

        public static void Check(ITypeInfo type, string assemblyName)
        {
            if (type.IsGenericParameter)
                return;

            //
            // In case of array/pointer types we have to inspect the element type
            //

            if (type.ElementType is not null)
            {
                Check(type.ElementType, assemblyName);
                return;
            }

            //
            // In case of generics we have to inspect all the generic arguments, too
            //

            if (type is IGenericTypeInfo genericType && !genericType.IsGenericDefinition)
            {
                foreach (ITypeInfo ga in genericType.GenericArguments)
                {
                    Check(ga, assemblyName);
                }

                Check(genericType.GenericDefinition, assemblyName);
                return;
            }

            if (type.DeclaringAssembly is null)
                throw new NotSupportedException();

            switch (type.AccessModifiers) 
            {
                case AccessModifiers.Private: case AccessModifiers.Protected when type.IsNested:
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