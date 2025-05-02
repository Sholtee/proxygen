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
                if (IsFriend())
                    return;

                if (!am.HasFlag(AccessModifiers.Protected) /*protected-internal*/)
                    ThrowIVTRequired();
            }

            if (am.HasFlag(AccessModifiers.Protected)) 
            {
                if (allowProtected)
                {
                    if (am.HasFlag(AccessModifiers.Private) /*private-protected*/ && !IsFriend())
                        ThrowIVTRequired();

                    return;
                }
                ThrowNotVisible();
            }

            //
            // Here the visibility can be either "Private" or "Explicit" -> HasFlag() not required
            //

            if (am is AccessModifiers.Explicit)
                return; // The method is visible after a type-cast

            if (am is AccessModifiers.Private)
                ThrowNotVisible();

            Assert(am is AccessModifiers.Public, $"Unknown AccessModifier: {am}");

            bool IsFriend() => method.DeclaringType.DeclaringAssembly?.IsFriend(assemblyName) is true;

            void ThrowNotVisible() => throw new MemberAccessException(string.Format(Resources.Culture, Resources.METHOD_NOT_VISIBLE, method.Name));

            void ThrowIVTRequired() => throw new MemberAccessException(string.Format(Resources.Culture, Resources.IVT_REQUIRED, method.Name, assemblyName));
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
            if (type.Flags.HasFlag(TypeInfoFlags.IsGenericParameter))
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
                    Check(ga, assemblyName);

                Check(genericType.GenericDefinition, assemblyName);
                return;
            }

            if (type.DeclaringAssembly is null)
                throw new NotSupportedException();

            switch (type.AccessModifiers) 
            {
                case AccessModifiers am when am.HasFlag(AccessModifiers.Internal):
                    if (!type.DeclaringAssembly.IsFriend(assemblyName))
                        throw new MemberAccessException(string.Format(Resources.Culture, Resources.IVT_REQUIRED, type, assemblyName));
                    break;
                case AccessModifiers am when am.HasFlag(AccessModifiers.Private) || am.HasFlag(AccessModifiers.Protected):
                    Assert(type.Flags.HasFlag(TypeInfoFlags.IsNested), "Only nested types can be declared 'private' or 'protected'");
                    throw new MemberAccessException(string.Format(Resources.Culture, Resources.TYPE_NOT_VISIBLE, type));
                default:
                    Assert(type.AccessModifiers is not AccessModifiers.Unknown, "Unknown access modifier");
                    break;
            }
        }
    }
}