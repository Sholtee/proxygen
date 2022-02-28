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
            ITypeInfo relatedGenerator,
            ReferenceCollector? referenceCollector): base(outputType, containingAssembly ?? $"Duck_{ITypeInfoExtensions.GetMD5HashCode(interfaceType, targetType)}", relatedGenerator, referenceCollector) 
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(interfaceType));

            InterfaceType = interfaceType;
            TargetType = targetType;

            //
            // Ne metadatabol generaljunk [MetadataTypeInfo.CreateFrom(typeof(DuckBase<>))).Close(targetType)] mert a "targetType" meg nem biztos h letezik
            //

            BaseType = ((IGenericTypeInfo) relatedGenerator.DeclaringAssembly!.GetType(typeof(DuckBase<>).FullName)!).Close(targetType);
            Target = BaseType
                .Properties
                .Single(prop => prop.Name == nameof(DuckBase<object>.Target))!;
        }

        //
        // Proxy egyseg mindig csak egy osztalyt definial
        //

        public override IReadOnlyCollection<string> DefinedClasses => new string[]
        {
            OutputType switch
            {
                OutputType.Unit => ContainingNameSpace + Type.Delimiter + ResolveClassName(null!),
                OutputType.Module => ResolveClassName(null!),
                _ => throw new NotSupportedException()
            }
        };

        public override CompilationUnitSyntax ResolveUnit(CancellationToken cancellation)
        {
            Visibility.Check(InterfaceType, ContainingAssembly);
            Visibility.Check(TargetType, ContainingAssembly);

            return base.ResolveUnit(cancellation);
        }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ITypeInfo> ResolveBases(object context) => new[] { BaseType, InterfaceType };

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ClassDeclarationSyntax> ResolveClasses(CancellationToken cancellation)
        {
            yield return ResolveClass(null!, cancellation);
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
              .ConvertAr(targetMember => targetMember, targetMember => !signatureEquals(targetMember, ifaceMember));

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
