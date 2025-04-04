/********************************************************************************
* PropertyInfoExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal static class PropertyInfoExtensions
    {
        public static PropertyInfo ExtractFrom<T>(Expression<Func<T, object>> expression) => (PropertyInfo) ((MemberExpression) expression.Body).Member;
    }
}
