using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace OMnG
{
    public abstract class ObjectExtensionsConfiguration
    {
        #region nested types

        public class DefaultConfiguration : DelegateILCachingConfiguration
        {

        }

        public class DelegateILCachingConfiguration : ObjectExtensionsConfiguration
        {
            private static Dictionary<Type, OpCode> ILTypeHash = new Dictionary<Type, OpCode>()
                {
                    { typeof(sbyte), OpCodes.Ldind_I1 },
                    { typeof(byte), OpCodes.Ldind_U1 },
                    { typeof(char), OpCodes.Ldind_U2 },
                    { typeof(short), OpCodes.Ldind_I2 },
                    { typeof(ushort), OpCodes.Ldind_U2 },
                    { typeof(int), OpCodes.Ldind_I4 },
                    { typeof(uint), OpCodes.Ldind_U4 },
                    { typeof(long), OpCodes.Ldind_I8 },
                    { typeof(ulong), OpCodes.Ldind_I8 },
                    { typeof(bool), OpCodes.Ldind_I1 },
                    { typeof(double), OpCodes.Ldind_R8 },
                    { typeof(float), OpCodes.Ldind_R4 }
                };

            private static Dictionary<PropertyInfo, Func<object, object>> Getters = new Dictionary<PropertyInfo, Func<object, object>>();
            private static Dictionary<PropertyInfo, Action<object, object>> Setters = new Dictionary<PropertyInfo, Action<object, object>>();

            public override object GetValue(PropertyInfo property, object target)
            {
                property = property ?? throw new ArgumentNullException(nameof(property));
                target = target ?? throw new ArgumentNullException(nameof(target));

                if (!property.CanRead)
                    throw new ArgumentException("The property cannot be read.", nameof(property));

                if (!Getters.ContainsKey(property))
                {
                    MethodInfo targetGetMethod = property.GetGetMethod(true);
                    DynamicMethod d = new DynamicMethod("", typeof(object), new[] { typeof(object) }, true);

                    ILGenerator ilGenerator = d.GetILGenerator();

                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Call, targetGetMethod);

                    Type returnType = targetGetMethod.ReturnType;

                    if (returnType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, returnType);
                    }

                    ilGenerator.Emit(OpCodes.Ret);

                    Getters.Add(property, (Func<object, object>)d.CreateDelegate(typeof(Func<object, object>)));

                    return property.GetValue(target);
                }
                return Getters[property](target);
            }
            public override void SetValue(PropertyInfo property, object target, object value)
            {
                property = property ?? throw new ArgumentNullException(nameof(property));
                target = target ?? throw new ArgumentNullException(nameof(target));

                if (!property.CanWrite)
                    throw new ArgumentException("The property cannot be set.", nameof(property));

                if (!Setters.ContainsKey(property))
                {
                    MethodInfo targetSetMethod = property.GetSetMethod(true);
                    DynamicMethod d = new DynamicMethod("", typeof(void), new[] { typeof(object), typeof(object) }, true);
                    ILGenerator ilGenerator = d.GetILGenerator();

                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldarg_1);

                    Type parameterType = property.PropertyType;

                    if (parameterType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Unbox_Any, parameterType);
                    }

                    ilGenerator.Emit(OpCodes.Call, targetSetMethod);
                    ilGenerator.Emit(OpCodes.Ret);
                    
                    Setters.Add(property, (Action<object, object>)d.CreateDelegate(typeof(Action<object, object>)));

                    property.SetValue(target, value);
                }
                Setters[property](target, value);
            }
        }
        public class DelegateCachingConfiguration : ObjectExtensionsConfiguration
        {
            private static Dictionary<PropertyInfo, Func<object, object>> Getters = new Dictionary<PropertyInfo, Func<object, object>>();
            private static Dictionary<PropertyInfo, Action<object, object>> Setters = new Dictionary<PropertyInfo, Action<object, object>>();

            public override object GetValue(PropertyInfo property, object target)
            {
                property = property ?? throw new ArgumentNullException(nameof(property));
                target = target ?? throw new ArgumentNullException(nameof(target));

                if (!property.CanRead)
                    throw new ArgumentException("The property cannot be read.", nameof(property));

                if (!Getters.ContainsKey(property))
                {
                    Delegate d = Delegate.CreateDelegate(
                        typeof(Func<,>).MakeGenericType(property.ReflectedType, property.PropertyType),
                        null,
                        property.GetGetMethod(true));

                    ParameterExpression targetParam = Expression.Parameter(typeof(object));
                    Func<object, object> fo =
                        Expression.Lambda<Func<object, object>>(
                            Expression.Convert(
                                Expression.Invoke(
                                    Expression.Convert(
                                        Expression.Constant(d),
                                        typeof(Func<,>).MakeGenericType(property.ReflectedType, property.PropertyType)),
                                    Expression.Convert(targetParam, property.ReflectedType)),
                            typeof(object)),
                        targetParam)
                        .Compile();

                    Getters.Add(property, fo);

                    return property.GetValue(target);
                }
                return Getters[property](target);
            }
            public override void SetValue(PropertyInfo property, object target, object value)
            {
                property = property ?? throw new ArgumentNullException(nameof(property));
                target = target ?? throw new ArgumentNullException(nameof(target));

                if (!property.CanWrite)
                    throw new ArgumentException("The property cannot be set.", nameof(property));

                if (!Setters.ContainsKey(property))
                {
                    Delegate d = Delegate.CreateDelegate(
                        typeof(Action<,>).MakeGenericType(property.ReflectedType, property.PropertyType),
                        null,
                        property.GetSetMethod(true));

                    ParameterExpression targetParam = Expression.Parameter(typeof(object));
                    ParameterExpression valueParam = Expression.Parameter(typeof(object));
                    Action<object, object> fo =
                        Expression.Lambda<Action<object, object>>(
                                Expression.Invoke(
                                    Expression.Convert(
                                        Expression.Constant(d),
                                        typeof(Action<,>).MakeGenericType(property.ReflectedType, property.PropertyType)
                                        ),
                                    Expression.Convert(targetParam, property.ReflectedType),
                                    Expression.Convert(valueParam, property.PropertyType)
                                ),
                        targetParam, valueParam)
                        .Compile();

                    Setters.Add(property, fo);

                    property.SetValue(target, value);
                }
                Setters[property](target, value);
            }
        }
        public class PureReflectionConfiguration : ObjectExtensionsConfiguration
        {
            public override object GetValue(PropertyInfo property, object target)
            {
                return property.GetValue(target);
            }
            public override void SetValue(PropertyInfo property, object target, object value)
            {
                property.SetValue(target, value);
            }
        }

        #endregion
        
        public abstract object GetValue(PropertyInfo property, object target);
        public abstract void SetValue(PropertyInfo property, object target, object value);
    }
}
