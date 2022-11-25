﻿/********************************************************************************
* MethodContext.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Solti.Utils.Proxy
{
    using Internals;

    /// <summary>
    /// Describes a method context.
    /// </summary>
    public class MethodContext 
    {
        /// <summary>
        /// Creates a new <see cref="MethodContext"/> instance.
        /// </summary>
        /// <remarks>Calling this constructor is time consuming operation. It is strongly advised to cache the created instances.</remarks>
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MethodContext(Func<object, object?[], object?> dispatch, IReadOnlyDictionary<MethodInfo, MethodInfo>? mappings)
        #pragma warning restore CS8618
        {
            if (dispatch is null)
                throw new ArgumentNullException(nameof(dispatch));

            ExtendedMemberInfo ifaceMember = MemberInfoExtensions.ExtractFrom(dispatch);

            InterfaceMember = ifaceMember.Member;
            InterfaceMethod = ifaceMember.Method;
            Dispatch = dispatch;

            if (mappings is not null)
            {
                if (mappings.TryGetValue(InterfaceMethod.IsGenericMethod ? InterfaceMethod.GetGenericMethodDefinition() : InterfaceMethod, out MethodInfo targetmethod))
                {
                    if (targetmethod.IsGenericMethod)
                        targetmethod = targetmethod.MakeGenericMethod(InterfaceMethod.GetGenericArguments());

                    TargeteMethod = targetmethod;
                    TargetMember = MemberInfoExtensions.ExtractFrom(targetmethod);
                }
                else
                    //
                    // Leave TargetXxX set to NULL instead of throwing as this method is being called in initializers.
                    // 

                    Trace.TraceWarning($"Cannot get target method for: {InterfaceMethod}");
            }
            else
            {
                TargeteMethod = InterfaceMethod;
                TargetMember = InterfaceMember;
            }
        }

        /// <summary>
        /// Creates a copy from the given source.
        /// </summary>
        protected MethodContext(MethodContext src)
        {
            if (src is null)
                throw new ArgumentNullException(nameof(src));

            InterfaceMember = src.InterfaceMember;
            InterfaceMethod = src.InterfaceMethod;
            TargetMember    = src.TargetMember;
            TargeteMethod   = src.TargeteMethod;
            Dispatch        = src.Dispatch;
        }

        /// <summary>
        /// The concrete method behind the <see cref="InterfaceMember"/>.
        /// </summary>
        public MethodInfo InterfaceMethod { get; }

        /// <summary>
        /// The member (property, event or method) that is being invoked.
        /// </summary>
        public MemberInfo InterfaceMember { get; }

        /// <summary>
        /// The concrete method behind the <see cref="TargetMember"/>.
        /// </summary>
        public MethodInfo TargeteMethod { get; }

        /// <summary>
        /// The member (property, event or method) that is being targeted.
        /// </summary>
        public MemberInfo TargetMember { get; }

        /// <summary>
        /// Gets the dispatcher function.
        /// </summary>
        public Func<object, object?[], object?> Dispatch { get; }
    }
}
