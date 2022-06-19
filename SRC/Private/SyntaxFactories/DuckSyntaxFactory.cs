/********************************************************************************
* DuckSyntaxFactory.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class DuckSyntaxFactory : ProxyUnitSyntaxFactory
    {
        public ITypeInfo InterfaceType { get; }

        public ITypeInfo TargetType { get; }

        public ITypeInfo BaseType { get; }

        public IPropertyInfo Target { get; }

        public DuckSyntaxFactory(
            ITypeInfo interfaceType,
            ITypeInfo targetType, 
            string? containingAssembly,
            OutputType outputType,
            IAssemblyInfo proxygenAsm,
            ReferenceCollector? referenceCollector): base(outputType, containingAssembly ?? $"Duck_{ITypeInfoExtensions.GetMD5HashCode(interfaceType, targetType)}", referenceCollector) 
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(interfaceType));

            InterfaceType = interfaceType;
            TargetType = targetType;

            //
            // BaseType beszerzeset nem tudjuk szebben megoldani mivel nem tudjuk h "targetType" mogott meta-adat vagy
            // szimbolum van.
            //

            BaseType = ((IGenericTypeInfo) proxygenAsm.GetType(typeof(DuckBase<>).FullName)!).Close(targetType);
            Target = BaseType
                .Properties
                .Single(static prop => prop.Name == nameof(DuckBase<object>.Target))!;
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
        protected override string ResolveClassName(object context) =>
            //
            // Az uj tipust egyertelmuen az interface es cel tipus hatarozza meg
            //

            $"Duck_{ITypeInfoExtensions.GetMD5HashCode(InterfaceType, TargetType)}";

        protected static TMember GetTargetMember<TMember>(TMember ifaceMember, IEnumerable<TMember> targetMembers, Func<TMember, TMember, bool> signatureEquals) where TMember : IMemberInfo
        {
            TMember[] possibleTargets = targetMembers
              .ConvertAr(static targetMember => targetMember, targetMember => !signatureEquals(targetMember, ifaceMember));

            if (!possibleTargets.Some())
                throw new MissingMemberException(string.Format(Resources.Culture, Resources.MISSING_IMPLEMENTATION, ifaceMember.Name));

            //
            // Lehet tobb implementacio is pl.:
            // "List<T>: ICollection<T>, IList" ahol IList nem ICollection<T> ose es mindkettonek van ReadOnly tulajdonsaga.
            //

            if (possibleTargets.Length > 1)
                throw new AmbiguousMatchException(string.Format(Resources.Culture, Resources.AMBIGUOUS_MATCH, ifaceMember.Name));

            return possibleTargets[0];
        }
    }
}
