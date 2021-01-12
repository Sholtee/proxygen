/********************************************************************************
* IMethodInfoExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal static class IMethodInfoExtensions
    {
        public static bool SignatureEquals(this IMethodInfo src, IMethodInfo that, bool ignoreVisibility = false)
        {
            if (!GetMethodBasicAttributes(src).Equals(GetMethodBasicAttributes(that)))
                return false;

            if (!Equals(src.ReturnValue, that.ReturnValue))
                return false;

            for (int i = 0; i < src.Parameters.Count; i++)
                if (!Equals(src.Parameters[i], that.Parameters[i]))
                    return false;

            return true;

            object GetMethodBasicAttributes(IMethodInfo m) => new
            {
                //
                // T ClassA<T>.Foo() != T ClassB<TT, T>.Foo()
                //

                DeclaringTypeArity = m.DeclaringType is IGenericTypeInfo declaringType && declaringType.IsGenericDefinition
                    ? declaringType.GenericArguments.Count
                    : 0,
                m.Name,
                m.IsStatic,
                m.IsSpecial,

                //
                // ClassA.Foo<T>() != ClassB.Foo<T, TT>() meg akkor sem ha nem nyitott generikusak
                //

                Arity = (m as IGenericMethodInfo)?.GenericArguments.Count,
                ParamCount = m.Parameters.Count,
                Accessibility = !ignoreVisibility
                    ? m.AccessModifiers
                    : (AccessModifiers?) null
            };

            static bool Equals(IParameterInfo p1, IParameterInfo p2) => p1.Kind == p2.Kind && p1.Type.EqualsTo(p2.Type);
        }
    }
}
