/********************************************************************************
* MetadataTypeInfo.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal class MetadataTypeInfo : ITypeInfo
    {
        protected Type UnderlyingType { get; }

        protected MetadataTypeInfo(Type underlyingType) => UnderlyingType = underlyingType;

        public static ITypeInfo CreateFrom(Type underlyingType) => underlyingType switch
        {
            _ when underlyingType.IsArray => new MetadataArrayTypeInfo(underlyingType),
            _ when underlyingType.GetOwnGenericArguments().Any() => new MetadataGenericTypeInfo(underlyingType),
            _ => new MetadataTypeInfo(underlyingType)
        };

        public override bool Equals(object obj) => obj is MetadataTypeInfo that && UnderlyingType.Equals(that.UnderlyingType);

        public override int GetHashCode() => UnderlyingType.GetHashCode();

        public override string ToString() => UnderlyingType.ToString();

        private IAssemblyInfo? FDeclaringAssembly;
        public IAssemblyInfo DeclaringAssembly => FDeclaringAssembly ??= MetadataAssemblyInfo.CreateFrom(UnderlyingType.Assembly);

        public bool IsVoid => UnderlyingType == typeof(void);

        private IReadOnlyList<ITypeInfo>? FEnclosingTypes;
        public IReadOnlyList<ITypeInfo> EnclosingTypes => FEnclosingTypes ??= UnderlyingType
            .GetEnclosingTypes()
            .Select(CreateFrom)
            .ToArray();

        private IReadOnlyList<ITypeInfo>? FInterfaces;
        public IReadOnlyList<ITypeInfo> Interfaces => FInterfaces ??= UnderlyingType
            .GetInterfaces()
            .Select(CreateFrom)
            .ToArray();

        private IReadOnlyList<ITypeInfo>? FBases;
        public IReadOnlyList<ITypeInfo> Bases => FBases ??= UnderlyingType
            .GetBaseTypes()
            .Select(CreateFrom)
            .ToArray();

        public virtual string Name => UnderlyingType.GetFriendlyName();

        public RefType RefType => UnderlyingType switch
        {
            // _ when UnderlyingType.IsByRef => RefType.Ref, // FIXME: ezt nem kene kikommentelni de ugy tunik a Type.IsByRef-nek nincs megfeleloje az INamedTypeInfo-ban (lasd: PassingByReference_ShouldNotAffectTheParameterType test)
            _ when UnderlyingType.IsPointer() => RefType.Pointer,
            _ => RefType.None
        };

        private ITypeInfo? FElementType;
        public ITypeInfo? ElementType
        {
            get
            {
                if (FElementType == null)
                {
                    Type? realType = UnderlyingType.GetElementType();

                    if (realType != null)
                        FElementType = CreateFrom(realType);
                }
                return FElementType;
            }
        }

        //
        // "Cica<T>.Mica<TT>"-nal a "TT" is beagyazott ami nekunk nem jo
        //

        public bool IsNested => UnderlyingType.IsNested() && !IsGenericParameter;

        public bool IsInterface => UnderlyingType.IsInterface();

        private IReadOnlyList<IPropertyInfo>? FProperties;
        public IReadOnlyList<IPropertyInfo> Properties => FProperties ??= UnderlyingType
            .ListMembers<PropertyInfo>(includeNonPublic: true /*explicit*/, includeStatic: true)
            .Where(p => p.GetMethod?.GetAccessModifiers() > AccessModifiers.Private || p.SetMethod?.GetAccessModifiers() > AccessModifiers.Private)
            .Distinct((a, b) => a.Name == b.Name && (a.GetMethod?.SignatureEquals(b.GetMethod!) == true || a.SetMethod?.SignatureEquals(b.SetMethod!) == true))
            .Select(MetadataPropertyInfo.CreateFrom)
            .ToArray();

        private IReadOnlyList<IEventInfo>? FEvents;
        public IReadOnlyList<IEventInfo> Events => FEvents ??= UnderlyingType
            .ListMembers<EventInfo>(includeNonPublic: true /*explicit*/, includeStatic: true)
            .Where(e => e.AddMethod?.GetAccessModifiers() > AccessModifiers.Private || e.RemoveMethod?.GetAccessModifiers() > AccessModifiers.Private)
            .Distinct((a, b) => a.Name == b.Name && (a.AddMethod?.SignatureEquals(b.AddMethod!) == true || a.RemoveMethod?.SignatureEquals(b.RemoveMethod!) == true))
            .Select(MetadataEventInfo.CreateFrom)
            .ToArray();

        //
        // Ezeket a metodusok forditas idoben nem leteznek igy a SymbolTypeInfo-ban sem fognak szerepelni.
        //

        private static readonly IReadOnlyList<Func<MethodInfo, bool>> MethodsToSkip = new Func<MethodInfo, bool>[] 
        {
            m => m.Name == "Finalize", // destructor
            m => m.DeclaringType.IsArray && m.Name == "Get", // = array[i]
            m => m.DeclaringType.IsArray && m.Name == "Set", // array[i] =
            m => m.DeclaringType.IsArray && m.Name == "Address", // = ref array[i]
            m => typeof(Delegate).IsAssignableFrom(m.DeclaringType) && m.Name == "Invoke" // delegate.Invoke(...)
        };

        private IReadOnlyList<IMethodInfo>? FMethods;
        public IReadOnlyList<IMethodInfo> Methods => FMethods ??= UnderlyingType
            .ListMembers<MethodInfo>(includeNonPublic: true /*explicit*/, includeStatic: true)
            .Where(m => m.GetAccessModifiers() != AccessModifiers.Private && !MethodsToSkip.Any(skip => skip(m)))

            //
            // Ez nem lehet a ListMEmbers()-ben elintezni (class A : B {} ahol class B {void Foo();}, ListMembers() Foo()-t
            // "A" es "B"-ben is megtalalja kulonbozo ReflectedType-al).
            //

            .Distinct((a, b) => a.SignatureEquals(b))
            .Select(MetadataMethodInfo.CreateFrom)
            .ToArray();

        private IReadOnlyList<IConstructorInfo>? FConstructors;
        public IReadOnlyList<IConstructorInfo> Constructors => FConstructors ??= 
            UnderlyingType.IsArray
                ? Array.Empty<IConstructorInfo>() // tomb egy geci specialis allatfaj: fordito generalja hozza a konstruktorokat -> futas idoben mar leteznek forditaskor meg nem
                : UnderlyingType
                    .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(ctor => ctor.GetAccessModifiers() != AccessModifiers.Private)
                    .Select(MetadataMethodInfo.CreateFrom)
                    .Cast<IConstructorInfo>()
                    .ToArray();

        public string? AssemblyQualifiedName => FullName is not null //  (UnderlyingType.IsGenericType ? UnderlyingType.GetGenericTypeDefinition() : UnderlyingType).AssemblyQualifiedName;
            ? $"{FullName}, {UnderlyingType.Assembly}"
            : null;

        public bool IsGenericParameter => UnderlyingType.IsGenericParameter();

        public string? FullName => UnderlyingType
            .GetFullName()
            ?.TrimEnd('&'); // FIXME: ez nem kene de ugy tunik a Type.IsByRef-nek nincs megfeleloje az INamedTypeInfo-ban

        private sealed class MetadataGenericTypeInfo : MetadataTypeInfo, IGenericTypeInfo
        {
            public MetadataGenericTypeInfo(Type underlyingType) : base(underlyingType) { }

            public bool IsGenericDefinition => UnderlyingType.IsGenericTypeDefinition;

            public IReadOnlyList<ITypeInfo> GenericArguments => UnderlyingType
                .GetOwnGenericArguments()
                .Select(CreateFrom)
                .ToArray();

            public override string Name => !UnderlyingType.IsGenericType || UnderlyingType.IsGenericTypeDefinition // FIXME: Type.GetFriendlyName() lezart generikusokat nem eszi meg (igaz elvileg nem is kell hivjuk lezart generikusra)
                ? base.Name
                : GenericDefinition.Name;

            public IGenericTypeInfo GenericDefinition => new MetadataGenericTypeInfo(UnderlyingType.GetGenericTypeDefinition());

            IGeneric IGeneric.GenericDefinition => GenericDefinition;

            public IGeneric Close(params ITypeInfo[] genericArgs)
            {
                if (UnderlyingType.IsNested) throw new NotSupportedException(); // TODO: implementalni ha hasznalni kell majd

                return (IGeneric) CreateFrom
                (
                    UnderlyingType.MakeGenericType
                    (
                        genericArgs
                            .Select(TypeInfoToMetadata)
                            .ToArray()
                    )
                );
            }
        }

        internal static Type TypeInfoToMetadata(ITypeInfo type)
        {
            //
            // Az AssemblyQualifiedName a nyilt generikus tipushoz tartozo nevet adja vissza
            //

            Type queried = Type.GetType(type.AssemblyQualifiedName, throwOnError: true);

            if (type is IGenericTypeInfo generic && generic.IsGenericDefinition)
                return queried;

            if (queried.IsGenericType)
            {
                Type[] gas = type
                    .EnclosingTypes
                    .Append(type)
                    .OfType<IGenericTypeInfo>()
                    .SelectMany(g => g.GenericArguments.Select(TypeInfoToMetadata))
                    .ToArray();

                return queried.MakeGenericType(gas);
            }

            return queried;
        }

        private sealed class MetadataArrayTypeInfo : MetadataTypeInfo, IArrayTypeInfo 
        {
            public MetadataArrayTypeInfo(Type underlyingType) : base(underlyingType) { }

            public int Rank => UnderlyingType.GetArrayRank();
        }
    }
}
