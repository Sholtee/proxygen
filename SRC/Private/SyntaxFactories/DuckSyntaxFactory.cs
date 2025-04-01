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

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal sealed partial class DuckSyntaxFactory : ProxyUnitSyntaxFactoryBase
    {
        private static string ResolveClassName(ITypeInfo interfaceType, ITypeInfo targetType) =>
            $"Duck_{new ITypeInfo[] { interfaceType, targetType }.GetMD5HashCode()}";

        public ITypeInfo InterfaceType { get; }

        public ITypeInfo TargetType { get; }

        public ITypeInfo BaseType { get; }

        public IPropertyInfo Target { get; }

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

            //
            // We don't know if BaseType should be backed by Symbol or Metadata, so grab it from proxyGenAsm.
            //

            BaseType = ((IGenericTypeInfo) proxyGenAsm.GetType(typeof(DuckBase<>).FullName)!).Close(targetType);
            Target = BaseType
                .Properties
                .Single(static prop => prop.Name == nameof(DuckBase<object>.Target));
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
        protected override IEnumerable<ITypeInfo> ResolveBases(object context) => new[] { BaseType, InterfaceType };

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
