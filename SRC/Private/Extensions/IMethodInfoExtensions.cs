/********************************************************************************
* IMethodInfoExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;

namespace Solti.Utils.Proxy.Internals
{
    internal static class IMethodInfoExtensions
    {
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
