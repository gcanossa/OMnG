using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OMnG
{
    public abstract class ObjectExtensionsConfiguration
    {
        #region nested types

        public class DefaultConfiguration : DelegateCachingConfiguration
        {

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
