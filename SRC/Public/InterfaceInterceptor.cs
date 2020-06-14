/********************************************************************************
* InterfaceInterceptor.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
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
    /// <remarks>This class is not thread safe even if the <see cref="InterfaceInterceptor{TInterface}.Target"/> is it.</remarks>
    public class InterfaceInterceptor<TInterface>: IHasTarget<TInterface?> where TInterface: class
    {
        /// <summary>
        /// Extracts the <see cref="MethodInfo"/> from the given expression.
        /// </summary>
        /// <param name="methodAccess">The expression to be processed.</param>
        /// <returns>The extracted <see cref="MethodInfo"/> instance.</returns>
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
        protected internal static readonly IReadOnlyDictionary<string, PropertyInfo> Properties = typeof(TInterface).ListMembers<PropertyInfo>()
            //
            // Tekintsuk a kovetkezot: IA: IB, IC ahol IB: IC -> Distinct()
            //
            .Distinct()
            .ToDictionary(prop => prop.GetFullName());

        /// <summary>
        /// All the <typeparamref name="TInterface"/> events.
        /// </summary>
        protected internal static readonly IReadOnlyDictionary<string, EventInfo> Events = typeof(TInterface).ListMembers<EventInfo>()
            .Distinct()
            .ToDictionary(ev => ev.GetFullName());

        /// <summary>
        /// The target of this interceptor.
        /// </summary>
        public TInterface? Target { get; }

        /// <summary>
        /// Invokes the original <see cref="Target"/> method.
        /// </summary>
        /// <remarks>Each intercepted method will have its own invocation.</remarks>
        protected internal Func<object>? InvokeTarget { get; set; }

        /// <summary>
        /// Creates a new <see cref="InterfaceInterceptor{TInterface}"/> instance against the given <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The target of this interceptor.</param>
        public InterfaceInterceptor(TInterface? target) => Target = target;

        /// <summary>
        /// Called on proxy method invocation.
        /// </summary>
        /// <param name="method">The <typeparamref name="TInterface"/> method that was called</param>
        /// <param name="args">The arguments passed by the caller to the intercepted method.</param>
        /// <param name="extra">Extra info about the member from which the <paramref name="method"/> was extracted.</param>
        /// <returns>The object to return to the caller, or null for void methods.</returns>
        public virtual object Invoke(MethodInfo method, object[] args, MemberInfo extra)
        {
            if (Target == null) throw new InvalidOperationException(Resources.NULL_TARGET);
            if (InvokeTarget == null) throw new InvalidOperationException(); // TODO

            try
            {
                return InvokeTarget();
            }
            finally 
            {
                InvokeTarget = null;
            }
        }
    }
}
