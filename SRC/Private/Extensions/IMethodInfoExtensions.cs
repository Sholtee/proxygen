/********************************************************************************
* IMethodInfoExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Solti.Utils.Proxy.Internals
{
    internal static class IMethodInfoExtensions
    {
        public static string GetMD5HashCode(this IMethodInfo src) => new IMethodInfo[] { src }.GetMD5HashCode();

        public static string GetMD5HashCode(this IEnumerable<IMethodInfo> methods)
        {
            using MD5 md5 = MD5.Create();

            foreach (IMethodInfo method in methods)
                method.Hash(md5);

            return md5.ToString("X2");
        }

        public static void Hash(this IMethodInfo src, ICryptoTransform transform)
        {
            byte[] name = Encoding.UTF8.GetBytes(src.Name);

            transform.TransformBlock(name, 0, name.Length, name, 0);

            //
            // IEnumerable<T>.GetEnumerator() - IEnumerable.GetEnumerator()
            //

            src.DeclaringType.Hash(transform);

            foreach (IParameterInfo param in src.Parameters)
                param.Type.Hash(transform);

            if (src is IGenericMethodInfo generic)
                foreach (ITypeInfo ga in generic.GenericArguments)
                    ga.Hash(transform);
        }

        public static bool SignatureEquals(this IMethodInfo src, IMethodInfo that, bool ignoreVisibility = false)
        {
            if (!GetMethodBasicAttributes(src).Equals(GetMethodBasicAttributes(that)))
                return false;

            if (!src.ReturnValue.EqualsTo(that.ReturnValue))
                return false;

            for (int i = 0; i < src.Parameters.Count; i++)
                if (!src.Parameters[i].EqualsTo(that.Parameters[i]))
                    return false;

            if (src is IGenericMethodInfo genericSrc && that is IGenericMethodInfo genericThat)
            {
                //
                // Due to the arity check there is no need for further validations.
                //

                if (genericSrc.GenericConstraints.Count != genericThat.GenericConstraints.Count)
                    return false;

                foreach (IGenericConstraint srcConstraint in genericSrc.GenericConstraints)
                    if (!genericThat.GenericConstraints.Any(srcConstraint.EqualsTo))
                        return false;
            }

            return true;

            object GetMethodBasicAttributes(IMethodInfo m) => new
            {
                //
                // T ClassA<T>.Foo() != T ClassB<TT, T>.Foo()
                //

                m.Name,
                m.IsStatic,
                m.IsSpecial,
                ParamCount = m.Parameters.Count,

                //
                // ClassA.Foo<T>() != ClassB.Foo<T, TT>()
                //

                Arity = (m as IGenericMethodInfo)?.GenericArguments.Count ?? -1, // new {xXx = (int?) 0} == new {xXx = (int?) null}
                Accessibility = !ignoreVisibility
                    ? m.AccessModifiers
                    : (AccessModifiers?) null
            };
        }

        public static IEnumerable<TMethodInfo> Sort<TMethodInfo>(this IEnumerable<TMethodInfo> self) where TMethodInfo: IMethodInfo => self
            .OrderBy(static m => $"{m.Name}_{m.GetMD5HashCode()}");
    }
}
