/********************************************************************************
* MethodContext.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
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
        public MethodContext(Func<object, object?[], object?> dispatch, IReadOnlyDictionary<MethodInfo, MethodInfo>? mappings)
        {
            if (dispatch is null)
                throw new ArgumentNullException(nameof(dispatch));

            ExtendedMemberInfo ifaceMember = MemberInfoExtensions.ExtractFrom(dispatch);

            InterfaceMember = ifaceMember.Member;
            InterfaceMethod = ifaceMember.Method;
            Dispatch = dispatch;

            if (mappings is not null)
            {
                TargeteMethod = mappings[InterfaceMethod];
                TargetMember = MemberInfoExtensions.ExtractFrom(TargeteMethod);
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
