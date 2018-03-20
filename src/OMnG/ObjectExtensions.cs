using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace OMnG
{
    public static class ObjectExtensions
    {
        public static IEnumerable<string> ToPropertyNameCollection<T>(this Expression<Func<T, object>> ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            NewExpression nexpr = ext.Body as NewExpression;

            Type baseType = typeof(T);

            if (nexpr != null)
            {
                foreach (PropertyInfo pinfo in nexpr.Members)
                {
                    PropertyInfo tmp = baseType.GetProperty(pinfo.Name);

                    if (tmp != null)
                        yield return pinfo.Name;
                }
            }
            else
            {
                MemberInfo info = (ext.Body as MemberExpression ?? ((UnaryExpression)ext.Body).Operand as MemberExpression).Member;

                PropertyInfo tmp = baseType.GetProperty(info.Name);

                if (tmp != null)
                    yield return info.Name;
            }
        }

        public static Dictionary<string, object> ToPropDictionary(this object ext)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            return ext.GetType().GetProperties().Where(p => p.CanRead).ToDictionary(p => p.Name, p => p.GetValue(ext));
        }
        
        public static Dictionary<string, object> ExludeProperties<T>(this T ext, Expression<Func<T, object>> selector)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return ext.ExludeProperties(selector.ToPropertyNameCollection());
        }
        public static Dictionary<string, object> ExludeProperties<T, E>(this T ext, Expression<Func<T, object>> additionalSelector = null)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext.ExludeProperties(typeof(E), additionalSelector);
        }
        public static Dictionary<string, object> ExludeProperties<T>(this T ext, Type selector, Expression<Func<T, object>> additionalSelector = null)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return ext.ExludeProperties(selector.GetProperties().Select(p => p.Name).Union(additionalSelector?.ToPropertyNameCollection() ?? new string[0]));
        }
        public static Dictionary<string, object> ExludeProperties<T>(this T ext, IEnumerable<string> properties)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string item in typeof(T).GetProperties().Select(p => p.Name).Except(properties))
            {
                result.Add(item, ext.GetPropValue(item));
            }

            return result;
        }
        public static Dictionary<string, object> ExludeProperties(this Dictionary<string, object> ext, IEnumerable<string> properties)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            return ext.Where(p => !properties.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);
        }

        public static Dictionary<string, object> ExludePrimitiveTypesProperties<T>(this T ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string item in typeof(T).GetProperties()
                .Where(p=> !p.PropertyType.IsValueType && p.PropertyType!=typeof(string))
                .Select(p=>p.Name))
            {
                result.Add(item, ext.GetPropValue(item));
            }

            return result;
        }
        public static Dictionary<string, object> ExludePrimitiveTypesProperties(this Dictionary<string, object> ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext
                .Where(p => p.Value != null)
                .Where(p => !p.Value.GetType().IsValueType && p.Value.GetType() != typeof(string)).ToDictionary(p => p.Key, p => p.Value);
        }
        public static Dictionary<string, object> ExludeCollectionTypesProperties<T>(this T ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string item in typeof(T).GetProperties()
                .Where(p => !IsCollection(p.PropertyType))
                .Select(p => p.Name))
            {
                result.Add(item, ext.GetPropValue(item));
            }

            return result;
        }
        public static Dictionary<string, object> ExludeCollectionTypesProperties(this Dictionary<string, object> ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext
                .Where(p => p.Value != null)
                .Where(p => !IsCollection(p.Value.GetType())).ToDictionary(p => p.Key, p => p.Value);
        }
        public static Dictionary<string, object> ExludeTypesProperties<T>(this T ext, params Type[] types)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string item in typeof(T).GetProperties()
                .Where(p => !types.Contains(p.PropertyType))
                .Select(p => p.Name))
            {
                result.Add(item, ext.GetPropValue(item));
            }

            return result;
        }
        public static Dictionary<string, object> ExludeTypesProperties(this Dictionary<string, object> ext, params Type[] types)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext
                .Where(p => p.Value != null)
                .Where(p => !types.Contains(p.Value.GetType())).ToDictionary(p => p.Key, p => p.Value);
        }

        public static Dictionary<string, object> SelectProperties<T>(this T ext, Expression<Func<T, object>> selector)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return ext.SelectProperties(selector.ToPropertyNameCollection());
        }
        public static Dictionary<string, object> SelectProperties<T, E>(this T ext, Expression<Func<T, object>> additionalSelector = null)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext.SelectProperties(typeof(E), additionalSelector);
        }
        public static Dictionary<string, object> SelectProperties<T>(this T ext, Type selector, Expression<Func<T, object>> additionalSelector = null)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return ext.SelectProperties(selector.GetProperties().Select(p => p.Name).Union(additionalSelector?.ToPropertyNameCollection() ?? new string[0]));
        }
        public static Dictionary<string, object> SelectProperties<T>(this T ext, IEnumerable<string> properties)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string item in properties)
            {
                result.Add(item, ext.GetPropValue(item));
            }

            return result;
        }
        public static Dictionary<string, object> SelectProperties(this Dictionary<string, object> ext, IEnumerable<string> properties)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            return ext.Where(p => properties.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);
        }

        public static Dictionary<string, object> SelectPrimitiveTypesProperties<T>(this T ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string item in typeof(T).GetProperties()
                .Where(p => p.PropertyType.IsValueType || p.PropertyType == typeof(string))
                .Select(p => p.Name))
            {
                result.Add(item, ext.GetPropValue(item));
            }

            return result;
        }
        public static Dictionary<string, object> SelectPrimitiveTypesProperties(this Dictionary<string, object> ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext
                .Where(p => p.Value == null || p.Value.GetType().IsValueType || p.Value.GetType() == typeof(string)).ToDictionary(p=>p.Key, p=>p.Value);
        }
        public static Dictionary<string, object> SelectCollectionTypesProperties<T>(this T ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string item in typeof(T).GetProperties()
                .Where(p => IsCollection(p.PropertyType))
                .Select(p => p.Name))
            {
                result.Add(item, ext.GetPropValue(item));
            }

            return result;
        }
        public static Dictionary<string, object> SelectCollectionTypesProperties(this Dictionary<string, object> ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext
                .Where(p => p.Value==null || IsCollection(p.Value.GetType())).ToDictionary(p => p.Key, p => p.Value);
        }
        public static Dictionary<string, object> SelectTypesProperties<T>(this T ext, params Type[] types)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string item in typeof(T).GetProperties()
                .Where(p => types.Contains(p.PropertyType))
                .Select(p => p.Name))
            {
                result.Add(item, ext.GetPropValue(item));
            }

            return result;
        }
        public static Dictionary<string, object> SelectTypesProperties(this Dictionary<string, object> ext, params Type[] types)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext
                .Where(p => p.Value == null || types.Contains(p.Value.GetType())).ToDictionary(p => p.Key, p => p.Value);
        }

        public static Dictionary<string, object> MergeWith<T>(this T ext, Func<T, object> selector)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return ext.MergeWith<T>(selector(ext));
        }
        public static Dictionary<string, object> MergeWith<T>(this T ext, object obj)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            obj = obj ?? new { };

            return ext.ToPropDictionary().MergeWith(obj.ToPropDictionary());
        }
        public static Dictionary<string, object> MergeWith(this Dictionary<string, object> ext, Dictionary<string, object> obj)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            obj = obj ?? new Dictionary<string, object>();

            foreach (KeyValuePair<string, object> kv in obj)
            {
                if (ext.ContainsKey(kv.Key))
                    ext[kv.Key] = kv.Value;
                else
                    ext.Add(kv.Key, kv.Value);
            }

            return ext;
        }

        public static bool HasPropery(this object ext, string name, Type baseType = null, Type propertyBaseType = null)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            name = name ?? throw new ArgumentNullException(nameof(name));
            baseType = baseType ?? ext.GetType();
            propertyBaseType = propertyBaseType ?? typeof(object);

            PropertyInfo pinfo = baseType.GetProperty(name);
            return pinfo != null && propertyBaseType.IsAssignableFrom(pinfo.PropertyType);
        }
        public static bool HasPropery<P>(this object ext, string name)
        {
            return ext.HasPropery(name, ext.GetType(), typeof(P));
        }

        public static object GetPropValue<T>(this T ext, string name)
        {
            return ext.GetPropValue<T, object>(name);
        }
        public static P GetPropValue<T, P>(this T ext, string name)
        {
            if (!ext.HasPropery<P>(name))
                throw new ArgumentException("Property not found.", nameof(name));

            return (P)ext.GetType().GetProperty(name).GetValue(ext);
        }

        public static void SetPropValue<P>(this object ext, string name, P value)
        {
            if (!ext.HasPropery<P>(name))
                throw new ArgumentException("Property not found.", nameof(name));

            ext.GetType().GetProperty(name).SetPropertyHelper(ext, value);
        }

        private static void SetPropertyHelper(this PropertyInfo pinfo, object ext, object value)
        {
            if (value == null)
                pinfo.SetValue(ext, GetDefault(pinfo.PropertyType));
            else
            {
                if (!pinfo.PropertyType.IsValueType)
                    pinfo.SetValue(ext, value);
                else if (IsDateTime(pinfo.PropertyType) && IsNumeric(value.GetType()))
                {
                    DateTimeOffset d = DateTimeOffset.FromUnixTimeMilliseconds((long)Convert.ChangeType(value, typeof(long)));

                    if (pinfo.PropertyType.IsAssignableFrom(typeof(DateTimeOffset)))
                        pinfo.SetValue(ext, d.ToLocalTime());
                    else
                        pinfo.SetValue(ext, d.ToLocalTime().DateTime);
                }
                else
                    pinfo.SetValue(ext, Convert.ChangeType(value, pinfo.PropertyType));
            }
        }

        public static T CopyProperties<T>(this T ext, Dictionary<string, object> from, Action<T> specialMappings = null)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            from = from ?? throw new ArgumentNullException(nameof(from));

            Type toType = ext.GetType();

            foreach (KeyValuePair<string, object> kv in from)
            {
                PropertyInfo pinfo = toType.GetProperty(kv.Key);

                pinfo?.SetPropertyHelper(ext, kv.Value);
            }

            specialMappings?.Invoke(ext);

            return ext;
        }
        public static T CopyProperties<T>(this T ext, object from, Action<T> specialMappings = null)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            from = from ?? throw new ArgumentNullException(nameof(from));

            Type toType = ext.GetType();

            foreach (PropertyInfo finfo in from.GetType().GetProperties())
            {
                PropertyInfo pinfo = toType.GetProperty(finfo.Name);

                pinfo?.SetPropertyHelper(ext, finfo.GetValue(from));
            }

            specialMappings?.Invoke(ext);

            return ext;
        }

        public static object GetDefault(Type type)
        {
            type = type ?? throw new ArgumentNullException(nameof(Type));
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static object GetInstanceOf(Type type, IDictionary<string, object> param)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));
            param = param ?? new Dictionary<string, object>();

            if (IsPrimitive(type))
            {
                return Convert.ChangeType(param.First().Value, type);
            }
            else
            {
                object[] args = GetParamsForConstructor(type, param);
                if (args != null)
                    return args.Length == 0 ?
                            Activator.CreateInstance(type, args).CopyProperties(param) :
                            Activator.CreateInstance(type, args);
            }

            throw new Exception("Unable to build an object of the desired type");
        }
        private static object[] GetParamsForConstructor(Type type, IDictionary<string, object> param)
        {
            if (type.GetConstructor(new Type[0]) != null)
                return new object[0];
            else
            {
                List<object> result = new List<object>();
                foreach (ConstructorInfo item in type.GetConstructors())
                {
                    List<ParameterInfo> tmp = item.GetParameters().ToList();

                    result.Clear();
                    foreach (ParameterInfo pinfo in tmp.ToList())
                    {
                        if (param.ContainsKey(pinfo.Name))
                        {
                            tmp.Remove(pinfo);
                            if (IsPrimitive(pinfo.ParameterType))
                                result.Add(Convert.ChangeType(param[pinfo.Name], pinfo.ParameterType));
                            else
                                result.Add(param[pinfo.Name]);
                        }
                    }
                    if (tmp.Count == 0)
                        return result.ToArray();
                }

                return null;
            }
        }

        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero) return dateTime; // Or could throw an ArgumentException
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }
        public static bool IsDateTime(this object ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return IsDateTime(ext.GetType());
        }
        public static bool IsDateTime(Type type)
        {
            type = type ?? throw new ArgumentNullException(nameof(Type));
            return type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateTime?) || type == typeof(DateTimeOffset?);
        }
        public static bool IsNumeric(this object ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return IsNumeric(ext.GetType());
        }
        public static bool IsNumeric(Type type)
        {
            type = type ?? throw new ArgumentNullException(nameof(Type));
            return type.IsPrimitive && type != typeof(char) && type != typeof(bool);
        }

        public static bool IsCollection(this object ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return IsCollection(ext.GetType());
        }
        public static bool IsCollection(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
        }
        
        public static bool IsPrimitive(this object ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return IsPrimitive(ext.GetType());
        }
        public static bool IsPrimitive(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return
                type.IsPrimitive ||
                new Type[] {
                    typeof(Enum),
                    typeof(String),
                    typeof(Decimal),
                    typeof(DateTime),
                    typeof(DateTimeOffset),
                    typeof(TimeSpan),
                    typeof(Guid)
                }.Contains(type) ||
                Convert.GetTypeCode(type) != TypeCode.Object ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsPrimitive(type.GetGenericArguments()[0]))
                ;
        }
    }
}
