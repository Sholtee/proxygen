/********************************************************************************
* DuckSyntaxFactory.DuckMemberSyntaxFactory.cs                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class DuckSyntaxFactory
    {
        internal abstract class DuckMemberSyntaxFactory : MemberSyntaxFactory
        {
            protected internal readonly IPropertyInfo TARGET;

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

            protected abstract IEnumerable<MemberDeclarationSyntax> Build();

            public IDuckContext Context { get; }

            public sealed override bool Build(CancellationToken cancellation)
            {
                if (Members is not null) return false;

                Members = Build().ToArray();

                return true;
            }

            public DuckMemberSyntaxFactory(IDuckContext context) : base(context.InterfaceType)
            {
                Context = context;

                TARGET = Context
                    .BaseType
                    .Properties
                    .Single(prop => prop.Name == nameof(DuckBase<object>.Target));
            }
        }
    }
}
