﻿/* Generic Builder base class
 * author: Davyd McColl (davydm@gmail.com)
 * license: BSD
 * */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using PeanutButter.TestUtils.Generic;
using PeanutButter.Utils;


namespace PeanutButter.RandomGenerators
{
    public interface IGenericBuilder
    {
        IGenericBuilder WithBuildLevel(int level);
        IGenericBuilder GenericWithRandomProps();
        object GenericBuild();
    }
    public class GenericBuilder<TConcrete, TEntity> : IGenericBuilder, IBuilder<TEntity> where TConcrete: GenericBuilder<TConcrete, TEntity>, new()
    {
        public static int MaxRandomPropsLevel { get; set; } = 10;
        private static readonly List<Action<TEntity>> _defaultPropMods = new List<Action<TEntity>>();
        private List<Action<TEntity>> _propMods = new List<Action<TEntity>>();

        public static TConcrete Create()
        {
            return new TConcrete();
        }

        public IGenericBuilder GenericWithRandomProps()
        {
            return _buildLevel > MaxRandomPropsLevel 
                        ? this 
                        : WithRandomProps();
        }

        public IGenericBuilder WithBuildLevel(int level)
        {
            _buildLevel = level;
            return this;
        }

        public object GenericBuild()
        {
            return Build();
        }

        public static TEntity BuildDefault()
        {
            return Create().Build();
        }

        public static TEntity BuildRandom()
        {
            return Create().WithRandomProps().Build();
        }

        public static void WithDefaultProp(Action<TEntity> action)
        {
            _defaultPropMods.Add(action);
        }

        public TConcrete WithProp(Action<TEntity> action)
        {
            _propMods.Add(action);
            return this as TConcrete;
        }

        public virtual TEntity ConstructEntity()
        {
            return (TEntity)Activator.CreateInstance(typeof (TEntity));
        }

        public virtual TEntity Build()
        {
            var entity = ConstructEntity();
            foreach (var action in _defaultPropMods.Union(_propMods))
            {
                action(entity);
            }
            return entity;
        }

        public virtual TConcrete WithRandomProps()
        {
            WithProp(SetRandomProps);
            return this as TConcrete;
        }

        private static object _lockObject = new object();
        private static PropertyInfo[] _EntityPropInfoField;
        private static PropertyInfo[] _EntityPropInfo
        {
            get
            {
                lock (_lockObject)
                {
                    if (_EntityPropInfoField == null)
                    {
                        _EntityPropInfoField = typeof(TEntity).GetProperties();
                    }
                    return _EntityPropInfoField;
                }
            }
        }

        private static Dictionary<Type, Type> _dynamicBuilders = new Dictionary<Type, Type>();
        private static Dictionary<Type, Type> _userBuilders = new Dictionary<Type, Type>(); 
        private static Dictionary<string, Action<TEntity, int>> _randomPropSettersField;
        private static Dictionary<string, Action<TEntity, int>> _randomPropSetters
        {
            get
            {
                var entityProps = _EntityPropInfo;
                lock (_lockObject)
                {
                    if (_randomPropSettersField == null)
                    {
                        _randomPropSettersField = new Dictionary<string, Action<TEntity, int>>();
                        foreach (var prop in entityProps)
                        {
                            SetSetterForType(prop);
                        }
                    }
                    return _randomPropSettersField;
                }
            }
        }

        private static readonly Dictionary<Type, Func<PropertyInfo, Action<TEntity, int>>> _simpleTypeSetters =
            new Dictionary<Type, Func<PropertyInfo, Action<TEntity, int>>>()
            {
                { typeof (int), pi => ((e, i) => pi.SetValue(e, RandomValueGen.GetRandomInt(), null))},
                { typeof (long), pi => ((e, i) => pi.SetValue(e, RandomValueGen.GetRandomInt(), null))},
                { typeof (float), pi => ((e, i) => pi.SetValue(e, Convert.ToSingle(RandomValueGen.GetRandomDouble(float.MinValue, float.MaxValue), null))) },
                { typeof (double), pi => ((e, i) => pi.SetValue(e, RandomValueGen.GetRandomDouble(), null))},
                { typeof (decimal), pi => ((e, i) => pi.SetValue(e, RandomValueGen.GetRandomDecimal(), null))},
                { typeof(DateTime), pi => ((e, i) => pi.SetValue(e, RandomValueGen.GetRandomDate(), null))},
                { typeof(Guid), pi => ((e, i) => pi.SetValue(e, Guid.NewGuid(), null)) },
                { typeof(string), CreateStringPropertyRandomSetterFor },
                { typeof(bool), CreateBooleanPropertyRandomSetterFor },
                { typeof(byte[]), pi => ((e, i) => pi.SetValue(e, RandomValueGen.GetRandomBytes(), null)) }
            };

        private static Action<TEntity, int> CreateStringPropertyRandomSetterFor(PropertyInfo pi)
        {
            if (MayBeEmail(pi))
                return (e, i) => pi.SetValue(e, RandomValueGen.GetRandomEmail(), null);
            if (MayBeUrl(pi))
                return (e, i) => pi.SetValue(e, RandomValueGen.GetRandomHttpUrl(), null);
            if (MayBePhone(pi))
                return (e, i) => pi.SetValue(e, RandomValueGen.GetRandomNumericString(), null);
            return (e, i) => pi.SetValue(e, RandomValueGen.GetRandomString(), null);
        }

        private static bool MayBePhone(PropertyInfo pi)
        {
            return pi != null &&
                   (pi.Name.ContainsOneOf("phone", "mobile", "fax") ||
                    pi.Name.StartsWithOneOf("tel"));
        }

        private static bool MayBeUrl(PropertyInfo pi)
        {
            return pi != null &&
                   pi.Name.ContainsOneOf("url", "website");
        }

        private static bool MayBeEmail(PropertyInfo pi)
        {
            return pi != null && pi.Name.ToLower().Contains("email");
        }

        private static Action<TEntity, int> CreateBooleanPropertyRandomSetterFor(PropertyInfo pi)
        {
            if (pi.Name == "Enabled")
                return (e, i) => pi.SetValue(e, true, null);
            return (e, i) => pi.SetValue(e, RandomValueGen.GetRandomBoolean(), null);
        }

        private static Type _nullableGeneric = typeof (Nullable<>);
        private static Type _collectionGeneric = typeof (ICollection<>);

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType &&
                    type.GetGenericTypeDefinition() == _nullableGeneric;
        }

        private static bool IsCollectionType(Type type)
        {
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition() == _collectionGeneric;
        }

        private static void SetSetterForType(PropertyInfo prop, Type propertyType = null)
        {
            if (!prop.CanWrite) return;
            propertyType = propertyType ?? prop.PropertyType;
            if (IsCollectionType(propertyType))
                return;

            Func<PropertyInfo, Action<TEntity, int>> setterGenerator;
            if (_simpleTypeSetters.TryGetValue(propertyType, out setterGenerator))
                _randomPropSetters[prop.Name] = setterGenerator(prop);
            else if (IsNullableType(propertyType))
            {
                var underlyingType = Nullable.GetUnderlyingType(propertyType);
                SetSetterForType(prop, underlyingType);
            }
            else
            {
                SetupBuilderSetterFor(prop);
            }
        }

        private static void SetupBuilderSetterFor(PropertyInfo prop)
        {
            var builderType = TryFindUserBuilderFor(prop.PropertyType)
                              ?? FindOrCreateDynamicBuilderFor(prop);
            _randomPropSettersField[prop.Name] = (e, i) =>
            {
                if (TraversedTooManyTurtles(i)) return;
                var dynamicBuilder = Activator.CreateInstance(builderType) as IGenericBuilder;
                prop.SetValue(e, dynamicBuilder.WithBuildLevel(i).GenericWithRandomProps().GenericBuild(), null);
            };
        }

        private static bool TraversedTooManyTurtles(int i)
        {
            var stackTrace = new StackTrace();
            var frames = stackTrace.GetFrames();
            if (HaveReenteredOwnRandomPropsTooManyTimesFor(frames))
                return true;
            return i > MaxRandomPropsLevel;
        }

        private static Type _genericBuilderBaseType = typeof(GenericBuilder<,>);
        private class TypeCallCounter
        {
            public Type Type { get; private set; }
            public int CallCount { get; set; }
            public TypeCallCounter(Type type)
            {
                Type = type;
                CallCount = 1;
            }
        }

        private static bool HaveReenteredOwnRandomPropsTooManyTimesFor(StackFrame[] frames)
        {
            var level = frames.Aggregate(0, (acc, cur) =>
            {
                var thisMethod = cur.GetMethod();
                var thisType = thisMethod.DeclaringType;
                if (thisType.IsGenericType && 
                        _genericBuilderBaseType.IsAssignableFrom(thisType) &&
                        thisMethod.Name == "SetRandomProps")
                {
                    return acc + 1;
                }
                return acc;
            });
            return level >= MaxRandomPropsLevel;
        }

        private static Type FindOrCreateDynamicBuilderFor(PropertyInfo propInfo)
        {
            Type builderType = null;
            if (_dynamicBuilders.TryGetValue(propInfo.PropertyType, out builderType))
                return builderType;
            return GenerateDynamicBuilderFor(propInfo);
        }

        private static Type TryFindUserBuilderFor(Type propertyType)
        {
            Type builderType = null;
            if (!_userBuilders.TryGetValue(propertyType, out builderType))
            {
                var existingBuilder = TryFindExistingBuilderFor(propertyType);
                if (existingBuilder != null)
                {
                    _userBuilders[propertyType] = builderType;
                    builderType = existingBuilder;
                }
            }
            return builderType;
        }

        private static Type TryFindExistingBuilderFor(Type propertyType)
        {
            // TODO: scour other assemblies for a possible builder (FUTURE, as required)
            return TryFindBuilderInCurrentAssemblyFor(propertyType)
                   ?? TryFindBuilderInAnyOtherAssemblyInAppDomainFor(propertyType);
        }

        private static Type[] TryGetExportedTypesFrom(Assembly asm)
        {
            try
            {
                return asm.GetExportedTypes();
            }
            catch
            {
                return new Type[] {};
            }
        }

        private static Type TryFindBuilderInAnyOtherAssemblyInAppDomainFor(Type propertyType)
        {
            try
            {
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a != propertyType.Assembly && !a.IsDynamic)
                    .SelectMany(TryGetExportedTypesFrom)
                    .Where(t => t.IsBuilderFor(propertyType));
                if (!types.Any())
                    return null;
                if (types.Count() == 1)
                    return types.First();
                return FindClosestNamespaceMatchFor(propertyType, types);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error whilst searching for user builder for type '" + propertyType.PrettyName() + "' in all loaded assemblies: " + ex.Message);
                return null;
            }
        }

        private static Type FindClosestNamespaceMatchFor(Type propertyType, IEnumerable<Type> types)
        {
            var seekNamespace = propertyType.Namespace.Split('.');
            return types.Aggregate((Type) null, (acc, cur) =>
            {
                if (acc == null)
                    return cur;
                var accParts = acc.Namespace.Split('.');
                var curParts = cur.Namespace.Split('.');
                var accMatchIndex = seekNamespace.MatchIndexFor(accParts);
                var curMatchIndex = seekNamespace.MatchIndexFor(curParts);
                return accMatchIndex < curMatchIndex ? acc : cur;
            });
        }

        private static Type TryFindBuilderInCurrentAssemblyFor(Type propType)
        {
            try
            {
                return propType.Assembly.GetTypes()
                    .FirstOrDefault(t => t.IsBuilderFor(propType));
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error whilst searching for user builder for type '" + propType.PrettyName() + "' in type's assembly: " + ex.Message);
                return null;
            }
        }

        private static Type GenerateDynamicBuilderFor(PropertyInfo prop)
        {
            var t = typeof(GenericBuilder<,>);
            var moduleName = string.Join("_", new[] { "DynamicEntityBuilders", prop.PropertyType.Name });
            var modBuilder = _dynamicAssemblyBuilder.DefineDynamicModule(moduleName);

            var typeBuilder = modBuilder.DefineType(prop.PropertyType + "Builder", TypeAttributes.Public | TypeAttributes.Class);
            // Typebuilder is a sub class of Type
            typeBuilder.SetParent(t.MakeGenericType(typeBuilder, prop.PropertyType));
            var dynamicBuilderType = typeBuilder.CreateType();
            _dynamicBuilders[prop.PropertyType] = dynamicBuilderType;
            return dynamicBuilderType;
        }

        private static object _dynamicAssemblyLock = new object();
        private static AssemblyBuilder _dynamicAssemblyBuilderField;
        private int _buildLevel;

        private static AssemblyBuilder _dynamicAssemblyBuilder
        {
            get
            {
                lock (_dynamicAssemblyLock)
                {
                    if (_dynamicAssemblyBuilderField == null)
                    {
                        _dynamicAssemblyBuilderField = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DynamicEntityBuilders"), 
                            AssemblyBuilderAccess.RunAndSave);
                    }
                    return _dynamicAssemblyBuilderField;
                }
            }
        }
        private void SetRandomProps(TEntity entity)
        {
            foreach (var prop in _EntityPropInfo)
            {
                try
                {
                    _randomPropSetters[prop.Name](entity, _buildLevel + 1);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unable to set random prop: {ex.Message}");
                }
            }
        }
    }
}
