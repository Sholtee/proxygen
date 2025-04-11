/********************************************************************************
* DuckSyntaxFactory.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal sealed partial class DuckSyntaxFactory : ProxyUnitSyntaxFactoryBase
    {
        private static string ResolveClassName(ITypeInfo interfaceType, ITypeInfo targetType) =>
            $"Duck_{new ITypeInfo[] { interfaceType, targetType }.GetMD5HashCode()}";

        private static readonly IPropertyInfo
            FTarget = MetadataPropertyInfo.CreateFrom
            (
                PropertyInfoExtensions.ExtractFrom(static (ITargetAccess ta) => ta.Target!)
            );

        private const string TARGET_FIELD = nameof(FTarget);

        private static MemberAccessExpressionSyntax GetTarget() => SimpleMemberAccess
        (
            ThisExpression(),
            TARGET_FIELD
        );

        public ITypeInfo InterfaceType { get; }

        public ITypeInfo TargetType { get; }

        public DuckSyntaxFactory
        (
            ITypeInfo interfaceType,
            ITypeInfo targetType, 
            string? containingAssembly,
            OutputType outputType,
            IAssemblyInfo proxyGenAsm,
            ReferenceCollector? referenceCollector = null,
            LanguageVersion languageVersion = LanguageVersion.Latest   
        )
        : base
        (
            outputType,
            containingAssembly ?? ResolveClassName(interfaceType, targetType),
            referenceCollector,
            languageVersion
        ) 
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(interfaceType));

            InterfaceType = interfaceType;
            TargetType = targetType;
        }

        public override CompilationUnitSyntax ResolveUnit(object context, CancellationToken cancellation)
        {
            Visibility.Check(InterfaceType, ContainingAssembly);
            Visibility.Check(TargetType, ContainingAssembly);

            return base.ResolveUnit(context, cancellation);
        }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ITypeInfo> ResolveBases(object context) => [InterfaceType, MetadataTypeInfo.CreateFrom(typeof(ITargetAccess))];

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ClassDeclarationSyntax> ResolveClasses(object context, CancellationToken cancellation)
        {
            yield return ResolveClass(context, cancellation);
        }

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveMembers(ClassDeclarationSyntax cls, object context, CancellationToken cancellation) => base.ResolveMembers
        (
            cls.AddMembers
            (
                ResolveField(TargetType, TARGET_FIELD, @static: false, @readonly: false)
            ),
            context,
            cancellation
        );

        #if DEBUG
        internal
        #endif
        protected override string ResolveClassName(object context) => ResolveClassName(InterfaceType, TargetType);

        private static TMember GetTargetMember<TMember>(TMember ifaceMember, IEnumerable<TMember> targetMembers, Func<TMember, TMember, bool> signatureEquals) where TMember : IMemberInfo
        {
            //
            // Starting from .NET7.0 interfaces may have abstract static members
            //

            if (ifaceMember.IsAbstract && ifaceMember.IsStatic)
                throw new NotSupportedException(Resources.ABSTRACT_STATIC_NOT_SUPPORTED);

            IReadOnlyList<TMember> possibleTargets = targetMembers
              .Where(targetMember => signatureEquals(targetMember, ifaceMember))
              .ToList();

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
