/********************************************************************************
* IGenericConstraintExtensions.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;

namespace Solti.Utils.Proxy.Internals
{
    internal static class IGenericConstraintExtensions
    {
        public static bool EqualsTo(this IGenericConstraint @this, IGenericConstraint that)
        {
            if (@this.ConstraintTypes.Count != that.ConstraintTypes.Count)
                return false;

            foreach (ITypeInfo c1 in @this.ConstraintTypes)
                if (!that.ConstraintTypes.Any(c1.EqualsTo))
                    return false;

            return 
                @this.Reference == that.Reference &&
                @this.DefaultConstructor == that.DefaultConstructor &&
                @this.Struct == that.Struct;
        }
    }
}
