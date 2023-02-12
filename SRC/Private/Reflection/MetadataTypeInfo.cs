/********************************************************************************
* MetadataTypeInfo.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal class MetadataTypeInfo : ITypeInfo
    {
        protected Type UnderlyingType { get; }

        protected MetadataTypeInfo(Type underlyingType) => UnderlyingType = underlyingType;

        public static ITypeInfo CreateFrom(Type underlyingType)
        {
            while (underlyingType.IsByRef)
            {
                underlyingType = underlyingType.GetElementType();
            }

            return underlyingType switch
            {
                _ when underlyingType.IsArray => new MetadataArrayTypeInfo(underlyingType),
                _ when underlyingType.GetOwnGenericArguments().Some() => new MetadataGenericTypeInfo(underlyingType),
                _ => new MetadataTypeInfo(underlyingType)
            };
        }

        public override bool Equals(object obj) => obj is MetadataTypeInfo that && UnderlyingType.Equals(that.UnderlyingType);

        public override int GetHashCode() => UnderlyingType.GetHashCode();

        public override string ToString() => UnderlyingType.ToString();

        private IAssemblyInfo? FDeclaringAssembly;
        public IAssemblyInfo DeclaringAssembly => FDeclaringAssembly ??= MetadataAssemblyInfo.CreateFrom(UnderlyingType.Assembly);

        public bool IsVoid => UnderlyingType == typeof(void);

        private ITypeInfo? FEnclosingType;
        public ITypeInfo? EnclosingType
        {
            get
            {
                if (FEnclosingType is null)
                {
                    Type? enclosingType = UnderlyingType.GetEnclosingType();

                    if (enclosingType is not null)
                        FEnclosingType = CreateFrom(enclosingType);
                }
                return FEnclosingType;
            }
        }

        private IReadOnlyList<ITypeInfo>? FInterfaces;
        public IReadOnlyList<ITypeInfo> Interfaces => FInterfaces ??= UnderlyingType.GetInterfaces().ConvertAr(CreateFrom);

        private ITypeInfo? FBaseType;
        public ITypeInfo? BaseType => UnderlyingType.BaseType is not null
            ? FBaseType ??= CreateFrom(UnderlyingType.BaseType)
            : null;

        public virtual string Name => UnderlyingType.GetFriendlyName();

        public RefType RefType => UnderlyingType switch
        {
#if NETSTANDARD2_1_OR_GREATER
            _ when UnderlyingType.IsByRefLike => RefType.Ref, // ref struct
#endif
            _ when UnderlyingType.IsPointer => RefType.Pointer,
            _ when UnderlyingType.IsArray => RefType.Array,
            _ => RefType.None
        };

        private ITypeInfo? FElementType;
        public ITypeInfo? ElementType
        {
            get
            {
                if (FElementType is null)
                {
                    Type? realType = UnderlyingType.GetElementType();

                    if (realType is not null)
                        FElementType = CreateFrom(realType);
                }
                return FElementType;
            }
        }

        //
        // "Cica<T>.Mica<TT>"-nal a "TT" is beagyazott ami nekunk nem jo
        //

        public bool IsNested => UnderlyingType.IsNested() && !IsGenericParameter;

        public bool IsInterface => UnderlyingType.IsInterface;

        private IReadOnlyList<IPropertyInfo>? FProperties;
        public IReadOnlyList<IPropertyInfo> Properties => FProperties ??= UnderlyingType
            .ListProperties(includeStatic: true)
            .ConvertAr(MetadataPropertyInfo.CreateFrom);

        private IReadOnlyList<IEventInfo>? FEvents;
        public IReadOnlyList<IEventInfo> Events => FEvents ??= UnderlyingType
            .ListEvents(includeStatic: true)
            .ConvertAr(MetadataEventInfo.CreateFrom);

        //
        // Ezeket a metodusok forditas idoben nem leteznek igy a SymbolTypeInfo-ban sem fognak szerepelni.
        //

        private static bool ShouldSkip(MethodInfo m) =>
            m.Name == "Finalize" || // destructor
            (m.DeclaringType.IsArray && m.Name == "Get") || // = array[i]
            (m.DeclaringType.IsArray && m.Name == "Set") ||  // array[i] =
            (m.DeclaringType.IsArray && m.Name == "Address") || // = ref array[i]
            (typeof(Delegate).IsAssignableFrom(m.DeclaringType) && m.Name == "Invoke") // delegate.Invoke(...)
#if DEBUG
            //
            // https://github.com/OpenCover/opencover/blob/master/main/OpenCover.Profiler/CodeCoverage_Cuckoo.cpp
            //

            || new string[] { "SafeVisited", "VisitedCritical" }.IndexOf(m.Name) >= 0
#endif
        ;

        private IReadOnlyList<IMethodInfo>? FMethods;
        public IReadOnlyList<IMethodInfo> Methods => FMethods ??= UnderlyingType
            .ListMethods(includeStatic: true)
            .ConvertAr(MetadataMethodInfo.CreateFrom, ShouldSkip);

        private IReadOnlyList<IConstructorInfo>? FConstructors;
        public IReadOnlyList<IConstructorInfo> Constructors => FConstructors ??= UnderlyingType.IsArray
            //
            // A tomb egy geci specialis allatfaj: fordito generalja hozza a konstruktorokat -> futas idoben mar leteznek forditaskor meg nem
            //

            ? Array.Empty<IConstructorInfo>()
            : UnderlyingType
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .ConvertAr(static ctor => (IConstructorInfo) MetadataMethodInfo.CreateFrom(ctor), static ctor => ctor.GetAccessModifiers() is AccessModifiers.Private);

        public string? AssemblyQualifiedName => QualifiedName is not null //  (UnderlyingType.IsGenericType ? UnderlyingType.GetGenericTypeDefinition() : UnderlyingType).AssemblyQualifiedName;
            ? $"{QualifiedName}, {UnderlyingType.Assembly}"
            : null;

        public bool IsGenericParameter => (UnderlyingType.GetInnermostElementType() ?? UnderlyingType).IsGenericParameter;

        public string? QualifiedName => UnderlyingType.GetQualifiedName();

        public bool IsClass => UnderlyingType.IsClass();

        public bool IsFinal => UnderlyingType.IsSealed;

        public bool IsAbstract => UnderlyingType.IsAbstract();

        private IHasName? FContainingMember;
        public IHasName? ContainingMember
        {
            get
            {
                if (FContainingMember is null)
                {
                    Type concreteType = UnderlyingType.GetInnermostElementType() ?? UnderlyingType;

                    FContainingMember = concreteType switch
                    {
                        _ when concreteType.IsGenericParameter /*kulonben a DeclaringMethod megbaszodik*/ && concreteType.DeclaringMethod is not null => MetadataMethodInfo.CreateFrom(concreteType.DeclaringMethod),
                        _ when /*Ez lehet T es nested is*/ concreteType.DeclaringType is not null => MetadataTypeInfo.CreateFrom(concreteType.DeclaringType),
                        _ => null
                    };
                }
                return FContainingMember;
            }
        }

        public AccessModifiers AccessModifiers => UnderlyingType.GetAccessModifiers();

        private sealed class MetadataGenericTypeInfo : MetadataTypeInfo, IGenericTypeInfo
        {
            public MetadataGenericTypeInfo(Type underlyingType) : base(underlyingType) { }

            public bool IsGenericDefinition
            {
                get
                {
                    foreach (Type ga in UnderlyingType.GetGenericArguments())
                    {
                        if (!ga.IsGenericParameter)
                            return false;
                    }
                    return true;
                }
            
            }
            private IReadOnlyList<ITypeInfo>? FGenericArguments;
            public IReadOnlyList<ITypeInfo> GenericArguments => FGenericArguments ??= UnderlyingType
                .GetOwnGenericArguments()
                .ConvertAr(CreateFrom);

            public override string Name => !UnderlyingType.IsGenericType || UnderlyingType.IsGenericTypeDefinition // FIXME: Type.GetFriendlyName() lezart generikusokat nem eszi meg (igaz elvileg nem is kell hivjuk lezart generikusra)
                ? base.Name
                : GenericDefinition.Name;

            public IGenericTypeInfo GenericDefinition => new MetadataGenericTypeInfo(UnderlyingType.GetGenericTypeDefinition());

            public IReadOnlyList<IGenericConstraint> GenericConstraints => throw new NotImplementedException();

            public IGenericTypeInfo Close(params ITypeInfo[] genericArgs)
            {
                if (UnderlyingType.IsNested)
                    throw new NotImplementedException(); // TODO

                Type[] gas = new Type[genericArgs.Length];

                for (int i = 0; i < genericArgs.Length; i++)
                {
                    gas[i] = genericArgs[i].ToMetadata();
                }

                return (IGenericTypeInfo) CreateFrom(UnderlyingType.MakeGenericType(gas));
            }
        }

        private sealed class MetadataArrayTypeInfo : MetadataTypeInfo, IArrayTypeInfo 
        {
            public MetadataArrayTypeInfo(Type underlyingType) : base(underlyingType) { }

            public int Rank => UnderlyingType.GetArrayRank();
        }
    }
}
