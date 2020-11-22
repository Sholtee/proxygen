/********************************************************************************
* DuckSyntaxFactory.InterceptorFactoryBase.cs                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class DuckSyntaxFactory<TInterface, TTarget>
    {
        internal abstract class InterceptorFactoryBase : DuckSyntaxFactory<TInterface, TTarget>, IInterceptorFactory
        {
            protected abstract bool SignatureEquals(IMemberInfo targetMember, IMemberInfo ifaceMember);

            protected TMember GetTargetMember<TMember>(TMember ifaceMember, IEnumerable<TMember> targetMembers) where TMember : IMemberInfo 
            {
                TMember[] possibleTargets = targetMembers
                  .Where(targetMember => SignatureEquals(targetMember, ifaceMember))
                  .ToArray();

                if (!possibleTargets.Any())
                    throw new MissingMemberException(string.Format(Resources.Culture, Resources.MISSING_IMPLEMENTATION, ifaceMember.Name));

                //
                // Lehet tobb implementacio is pl.:
                // "List<T>: ICollection<T>, IList" ahol IList nem ICollection<T> ose es mindkettonek van ReadOnly tulajdonsaga.
                //

                if (possibleTargets.Length > 1)
                    throw new AmbiguousMatchException(string.Format(Resources.Culture, Resources.AMBIGUOUS_MATCH, ifaceMember.Name));

                return possibleTargets[0];
            }

            public virtual bool IsCompatible(IMemberInfo member) => member.DeclaringType.IsInterface;

            public abstract MemberDeclarationSyntax Build(IMemberInfo member);
        }
    }
}
