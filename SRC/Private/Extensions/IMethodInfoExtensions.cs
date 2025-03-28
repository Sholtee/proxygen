/********************************************************************************
* IMethodInfoExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Solti.Utils.Proxy.Internals
{
    internal static class IMethodInfoExtensions
    {
        public static string GetMD5HashCode(this IMethodInfo src)
        {
            using MD5 md5 = MD5.Create();

            byte[] name = Encoding.UTF8.GetBytes(src.Name);

            md5.TransformBlock(name, 0, name.Length, name, 0);

            //
            // IEnumerable<T>.GetEnumerator() - IEnumerable.GetEnumerator()
            //

            src.DeclaringType.Hash(md5); 

            foreach (IParameterInfo param in src.Parameters)
                param.Type.Hash(md5);

            if (src is IGenericMethodInfo generic)
                foreach (ITypeInfo ga in generic.GenericArguments)
                    ga.Hash(md5);

            md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            StringBuilder sb = new();

            for (int i = 0; i < md5.Hash.Length; i++)
            {
                sb.Append(md5.Hash[i].ToString("X2", null));
            }

            return sb.ToString();
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
    }
}
