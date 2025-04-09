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
        public static PropertyInfo ExtractFrom<T, TResult>(Expression<Func<T, TResult>> expression) => (PropertyInfo) ((MemberExpression) expression.Body).Member;
    }
}
