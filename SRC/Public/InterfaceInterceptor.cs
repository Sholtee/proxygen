﻿/********************************************************************************
* InterfaceInterceptor.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal static MethodInfo ResolveMethod(Expression<Action> methodAccess) => 
            (MethodInfo) MemberInfoExtensions.ExtractFrom(methodAccess ?? throw new ArgumentNullException(nameof(methodAccess)));

        private static readonly IReadOnlyDictionary<int, MemberInfo> FMembers = typeof(TInterface)
            .ListMembers<MemberInfo>()
            .Distinct()
            .ToDictionary(x => x.MetadataToken);

        /// <summary>
        /// Resolves a member by its metadata token.
        /// </summary>
        /// <remarks>This is an internal method, don't use it.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal static MemberInfo ResolveMember(int metadataToken) => FMembers[metadataToken]; // typeof(TInterface).Module.ResolveMember(metadataToken);

        /// <summary>
        /// Extracts the <see cref="PropertyInfo"/> from the given expression.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal static PropertyInfo ResolveProperty(Func<object?> propertyAccess) => // nem lehet expression: https://docs.microsoft.com/en-us/dotnet/csharp/misc/cs0832
            (PropertyInfo) MemberInfoExtensions.ExtractFrom((propertyAccess ?? throw new ArgumentNullException(nameof(propertyAccess))).Method, MemberTypes.Property)!;

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
        public virtual object? Invoke(MethodInfo method, object?[] args, MemberInfo extra)
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
