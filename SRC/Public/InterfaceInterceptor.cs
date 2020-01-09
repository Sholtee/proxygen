/********************************************************************************
* InterfaceInterceptor.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Proxy
{
    using Properties;
    using Internals;

    /// <summary>
    /// Provides the mechanism for intercepting interface method calls.
    /// </summary>
    /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
    public class InterfaceInterceptor<TInterface>: IHasTarget<TInterface> where TInterface: class
    {
        /// <summary>
        /// Signals that the original method should be called.
        /// </summary>
        /// <remarks>Internal, don't use it!</remarks>
        [SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Descendants need direct access to the field")]
        protected internal readonly object CALL_TARGET = new object();

        /// <summary>
        /// Extracts the <see cref="MethodInfo"/> from the given expression.
        /// </summary>
        /// <param name="methodAccess">The expression to be process.</param>
        /// <returns>The extracted <see cref="MethodInfo"/> object.</returns>
        /// <remarks>This is an internal method, don't use it.</remarks>
        protected internal static MethodInfo MethodAccess(Expression<Action> methodAccess)
        {
            if (methodAccess == null)
                throw new ArgumentNullException(nameof(methodAccess));

            return (MethodInfo) MemberInfoExtensions.ExtractFrom(methodAccess);
        }

        //
        // Ez itt NEM mukodik write-only property-kre
        //
/*
        protected internal static PropertyInfo PropertyAccess<TResult>(Expression<Func<TResult>> propertyAccess) => (PropertyInfo) ((MemberExpression) propertyAccess.Body).Member;
*/
        //
        // Ez mukodne viszont forditas ideju kifejezesek nem tartalmazhatnak ertekadast (lasd: http://blog.ashmind.com/2007/09/07/expression-tree-limitations-in-c-30/) 
        // tehat pl: "() => i.Prop = 0" hiaba helyes nem fog fordulni.
        //
/*
        protected internal static PropertyInfo PropertyAccess(Expression<Action> propertyAccess) => (PropertyInfo) ((MemberExpression) ((BinaryExpression) propertyAccess.Body).Left).Member;
*/
        //
        // Szoval marad a mersekelten szep megoldas (esemenyeket pedig amugy sem lehet kitalalni kifejezesek segitsegevel):
        //

        /// <summary>
        /// All the <typeparamref name="TInterface"/> properties.
        /// </summary>
        protected internal static readonly IReadOnlyDictionary<string, PropertyInfo> Properties = typeof(TInterface).ListMembers(System.Reflection.TypeExtensions.GetProperties)
            .ToDictionary(prop => prop.GetFullName());

        /// <summary>
        /// All the <typeparamref name="TInterface"/> events.
        /// </summary>
        protected internal static readonly IReadOnlyDictionary<string, EventInfo> Events = typeof(TInterface).ListMembers(System.Reflection.TypeExtensions.GetEvents)
            .ToDictionary(ev => ev.GetFullName());

        /// <summary>
        /// The target of this interceptor.
        /// </summary>
        public TInterface Target { get; }

        /// <summary>
        /// Creates a new <see cref="InterfaceInterceptor{TInterface}"/> instance against the given <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The target of this interceptor.</param>
        /// <remarks>The interceptor must have only one public constructor.</remarks>
        public InterfaceInterceptor(TInterface target) => Target = target;

        /// <summary>
        /// Called on proxy method invocation.
        /// </summary>
        /// <param name="method">The <typeparamref name="TInterface"/> method that was called</param>
        /// <param name="args">The arguments passed by the caller to the intercepted method.</param>
        /// <param name="extra">Extra info about the member from which the <paramref name="method"/> was extracted.</param>
        /// <returns>The object to return to the caller, or null for void methods.</returns>
        /// <remarks>The invocation will be forwarded to the <see cref="Target"/> if this method returns <see cref="CALL_TARGET"/>.</remarks>
        public virtual object Invoke(MethodInfo method, object[] args, MemberInfo extra) => Target != null ? CALL_TARGET : throw new InvalidOperationException(Resources.NULL_TARGET);
    }
}
