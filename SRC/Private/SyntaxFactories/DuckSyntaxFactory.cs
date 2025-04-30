/********************************************************************************
* DuckSyntaxFactory.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal sealed partial class DuckSyntaxFactory : ProxyUnitSyntaxFactoryBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ITypeInfo FInterfaceType;

        public override string ExposedClass => $"Duck_{new ITypeInfo[] { FInterfaceType, TargetType! }.GetMD5HashCode()}";

        public DuckSyntaxFactory(ITypeInfo interfaceType, ITypeInfo targetType, SyntaxFactoryContext context) : base(targetType, context) 
        {
            if (!interfaceType.Flags.HasFlag(TypeInfoFlags.IsInterface))
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(interfaceType));

            FInterfaceType = interfaceType;
        }

        #if DEBUG
        internal
        #endif
        protected override CompilationUnitSyntax ResolveUnitCore(object context, CancellationToken cancellation)
        {
            Visibility.Check(FInterfaceType, ContainingAssembly);
            return base.ResolveUnitCore(context, cancellation);
        }

        #if DEBUG
        internal
        #endif
        protected override IReadOnlyList<ITypeInfo> Bases => [FInterfaceType];

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ClassDeclarationSyntax> ResolveClasses(object context, CancellationToken cancellation)
        {
            yield return ResolveClass(context, cancellation);
        }

        private static TMember GetTargetMember<TMember>(TMember ifaceMember, IEnumerable<TMember> targetMembers, Func<TMember, TMember, bool> signatureEquals) where TMember : IMemberInfo
        {
            //
            // Starting from .NET7.0 interfaces may have abstract static members
            //

            if (ifaceMember.IsAbstract && ifaceMember.IsStatic)
                throw new NotSupportedException(Resources.ABSTRACT_STATIC_NOT_SUPPORTED);

            IReadOnlyList<TMember> possibleTargets = [..targetMembers.Where(targetMember => signatureEquals(targetMember, ifaceMember))];

            return possibleTargets.Count switch
            {
                0 => throw new MissingMemberException(string.Format(Resources.Culture, Resources.MISSING_IMPLEMENTATION, ifaceMember.Name)),
                1 => possibleTargets[0],

                //
                // There might be multiple implementations:
                // "List<T>: ICollection<T>, IList" [both ICollection<T> and IList has ReadOnly property].
                //

                _ => throw new AmbiguousMatchException(string.Format(Resources.Culture, Resources.AMBIGUOUS_MATCH, ifaceMember.Name))
            };
        }
    }
}
