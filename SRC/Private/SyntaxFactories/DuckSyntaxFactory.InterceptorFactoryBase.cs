/********************************************************************************
* DuckSyntaxFactory.InterceptorFactoryBase.cs                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class DuckSyntaxFactory<TInterface, TTarget>
    {
        internal abstract class InterceptorFactoryBase : IInterceptorFactory
        {
            public DuckSyntaxFactory<TInterface, TTarget> Owner { get; }

            public InterceptorFactoryBase(DuckSyntaxFactory<TInterface, TTarget> owner) => Owner = owner;

            protected abstract bool SignatureEquals(MemberInfo targetMember, MemberInfo ifaceMember);

            protected TMember GetTargetMember<TMember>(TMember ifaceMember) where TMember : MemberInfo 
            {
                TMember[] possibleTargets = typeof(TTarget)
                  .ListMembers<TMember>(includeNonPublic: true)
                  .Where(targetMember => SignatureEquals(targetMember, ifaceMember))
                  .ToArray();

                if (!possibleTargets.Any())
                    throw new MissingMemberException(string.Format(Resources.Culture, Resources.MISSING_IMPLEMENTATION, ifaceMember.GetFullName()));

                //
                // Lehet tobb implementacio is pl.:
                // "List<T>: ICollection<T>, IList" ahol IList nem ICollection<T> ose es mindkettonek van ReadOnly tulajdonsaga.
                //

                if (possibleTargets.Length > 1)
                    throw new AmbiguousMatchException(string.Format(Resources.Culture, Resources.AMBIGUOUS_MATCH, ifaceMember.GetFullName()));

                return possibleTargets[0];
            }

            public abstract MemberDeclarationSyntax Build(MemberInfo member);

            public abstract bool IsCompatible(MemberInfo member);
        }
    }
}
