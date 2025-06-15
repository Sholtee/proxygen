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
    /// <summary>
    /// Helper methods for the <see cref="PropertyInfo"/> class
    /// </summary>
    internal static class PropertyInfoExtensions
    {
        /// <summary>
        /// Extracts the referenced <see cref="PropertyInfo"/> instance from the given <paramref name="expression"/>.
        /// </summary>
        public static PropertyInfo ExtractFrom<T, TResult>(Expression<Func<T, TResult>> expression) => (PropertyInfo) ((MemberExpression) expression.Body).Member;
    }
}
