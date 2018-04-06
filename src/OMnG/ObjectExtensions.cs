using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;

namespace OMnG
{
    public static class ObjectExtensions
    {
        private static ObjectExtensionsConfiguration DefaultConfiguration = new ObjectExtensionsConfiguration.DefaultConfiguration();

        [ThreadStatic]
        internal static Stack<ObjectExtensionsConfiguration> _configuration;
        public static ObjectExtensionsConfiguration Configuration
        {
            get
            {
                _configuration = _configuration ?? new Stack<ObjectExtensionsConfiguration>();
                if (_configuration.Count == 0)
                    _configuration.Push(DefaultConfiguration);
                return _configuration.Peek();
            }
        }

        public static IDisposable ConfigScope(ObjectExtensionsConfiguration configuration)
        {
            configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _configuration.Push(configuration);

            return new CustomDisposable(()=>_configuration.Pop());
        }

        public static IEnumerable<string> ToPropertyNameCollection<T, R>(this Expression<Func<T, R>> ext) where T : class
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

        public static IDictionary<string, object> ToPropDictionary(this object ext)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            if (typeof(IDictionary<string, object>).IsAssignableFrom(ext.GetType()))
                throw new ArgumentException("ext is already a dictionary");

            return ext.GetType().GetProperties().Where(p => p.CanRead).ToDictionary(p => p.Name, p => Configuration.Get(p,ext));
        }
        
        public static IDictionary<string, object> ExludeProperties<T>(this T ext, Expression<Func<T, object>> selector) where T : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return ext.ExludeProperties(selector.ToPropertyNameCollection());
        }
        public static IDictionary<string, object> ExludeProperties<T, E>(this T ext, Expression<Func<T, object>> additionalSelector = null)
             where T : class
             where E : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext.ExludeProperties(typeof(E), additionalSelector);
        }
        public static IDictionary<string, object> ExludeProperties<T>(this T ext, Type selector, Expression<Func<T, object>> additionalSelector = null) where T : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return ext.ExludeProperties(selector.GetProperties().Select(p => p.Name).Union(additionalSelector?.ToPropertyNameCollection() ?? new string[0]));
        }
        public static IDictionary<string, object> ExludeProperties<T>(this T ext, IEnumerable<string> properties)
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
        public static IDictionary<string, object> ExludeProperties(this IDictionary<string, object> ext, IEnumerable<string> properties)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            return ext.Where(p => !properties.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);
        }

        public static IDictionary<string, object> ExludePrimitiveTypesProperties<T>(this T ext) where T : class
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
        public static IDictionary<string, object> ExludePrimitiveTypesProperties(this IDictionary<string, object> ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext
                .Where(p => p.Value != null)
                .Where(p => !p.Value.GetType().IsValueType && p.Value.GetType() != typeof(string)).ToDictionary(p => p.Key, p => p.Value);
        }
        public static IDictionary<string, object> ExludeCollectionTypesProperties<T>(this T ext) where T : class
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
        public static IDictionary<string, object> ExludeCollectionTypesProperties(this IDictionary<string, object> ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext
                .Where(p => p.Value != null)
                .Where(p => !IsCollection(p.Value.GetType())).ToDictionary(p => p.Key, p => p.Value);
        }
        public static IDictionary<string, object> ExludeTypesProperties<T>(this T ext, params Type[] types) where T : class
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
        public static IDictionary<string, object> ExludeTypesProperties(this IDictionary<string, object> ext, params Type[] types)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext
                .Where(p => p.Value != null)
                .Where(p => !types.Contains(p.Value.GetType())).ToDictionary(p => p.Key, p => p.Value);
        }
        public static IDictionary<string, object> ExludeReadonlyProperties<T>(this T ext) where T : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string item in typeof(T).GetProperties()
                .Where(p => p.CanWrite && p.CanRead)
                .Select(p => p.Name))
            {
                result.Add(item, ext.GetPropValue(item));
            }

            return result;
        }

        public static IDictionary<string, object> SelectProperties<T>(this T ext, Expression<Func<T, object>> selector) where T : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return ext.SelectProperties(selector.ToPropertyNameCollection());
        }
        public static IDictionary<string, object> SelectProperties<T, E>(this T ext, Expression<Func<T, object>> additionalSelector = null) 
            where T : class 
            where E : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext.SelectProperties(typeof(E), additionalSelector);
        }
        public static IDictionary<string, object> SelectProperties<T>(this T ext, Type selector, Expression<Func<T, object>> additionalSelector = null) where T : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return ext.SelectProperties(selector.GetProperties().Select(p => p.Name).Union(additionalSelector?.ToPropertyNameCollection() ?? new string[0]));
        }
        public static IDictionary<string, object> SelectProperties<T>(this T ext, IEnumerable<string> properties) where T : class
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
        public static IDictionary<string, object> SelectProperties(this IDictionary<string, object> ext, IEnumerable<string> properties)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            return ext.Where(p => properties.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);
        }

        public static IDictionary<string, object> SelectPrimitiveTypesProperties<T>(this T ext) where T : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string item in typeof(T).GetProperties()
                .Where(p => IsPrimitive(p.PropertyType))
                .Select(p => p.Name))
            {
                result.Add(item, ext.GetPropValue(item));
            }

            return result;
        }
        public static IDictionary<string, object> SelectPrimitiveTypesProperties(this IDictionary<string, object> ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext
                .Where(p => p.Value == null || IsPrimitive(p.Value)).ToDictionary(p=>p.Key, p=>p.Value);
        }
        public static IDictionary<string, object> SelectCollectionTypesProperties<T>(this T ext) where T : class
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
        public static IDictionary<string, object> SelectCollectionTypesProperties(this IDictionary<string, object> ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext
                .Where(p => p.Value==null || IsCollection(p.Value.GetType())).ToDictionary(p => p.Key, p => p.Value);
        }
        public static IDictionary<string, object> SelectTypesProperties<T>(this T ext, params Type[] types) where T : class
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
        public static IDictionary<string, object> SelectTypesProperties(this IDictionary<string, object> ext, params Type[] types)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return ext
                .Where(p => p.Value == null || types.Contains(p.Value.GetType())).ToDictionary(p => p.Key, p => p.Value);
        }
        public static IDictionary<string, object> SelectReadonlyProperties<T>(this T ext) where T : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string item in typeof(T).GetProperties()
                .Where(p => !p.CanWrite && p.CanRead)
                .Select(p => p.Name))
            {
                result.Add(item, ext.GetPropValue(item));
            }

            return result;
        }

        public static IDictionary<string, object> MergeWith<T>(this T ext, Func<T, object> selector) where T : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return ext.MergeWith<T>(selector(ext));
        }
        public static IDictionary<string, object> MergeWith<T>(this T ext, object obj) where T : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            obj = obj ?? new { };

            return ((IDictionary<string, object>)ext.ToPropDictionary()).MergeWith(obj.ToPropDictionary());
        }
        public static IDictionary<string, object> MergeWith(this IDictionary<string, object> ext, IDictionary<string, object> obj)
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

            return (P)Configuration.Get(ext.GetType().GetProperty(name),ext);
        }

        public static void SetPropValue<P>(this object ext, string name, P value)
        {
            if (!ext.HasPropery<P>(name))
                throw new ArgumentException("Property not found.", nameof(name));

            Configuration.Set(ext.GetType().GetProperty(name), ext, value);
        }

        public static T CopyProperties<T>(this T ext, IDictionary<string, object> from, Action<T> specialMappings = null) where T : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            from = from ?? throw new ArgumentNullException(nameof(from));

            Type toType = ext.GetType();

            foreach (KeyValuePair<string, object> kv in from)
            {
                PropertyInfo pinfo = toType.GetProperty(kv.Key);

                Configuration.Set(pinfo, ext, kv.Value);
            }

            specialMappings?.Invoke(ext);

            return ext;
        }
        public static T CopyProperties<T>(this T ext, object from, Action<T> specialMappings = null) where T : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            from = from ?? throw new ArgumentNullException(nameof(from));

            Type toType = ext.GetType();

            foreach (PropertyInfo finfo in from.GetType().GetProperties())
            {
                PropertyInfo pinfo = toType.GetProperty(finfo.Name);

                Configuration.Set(pinfo, ext, Configuration.Get(finfo, from));
            }

            specialMappings?.Invoke(ext);

            return ext;
        }

        public static object GetDefault(Type type)
        {
            type = type ?? throw new ArgumentNullException(nameof(Type));
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static object GetInstanceOfMostSpecific(this IEnumerable<Type> types)
        {
            types = types ?? throw new ArgumentNullException(nameof(types));

            Type type = null;
            foreach (Type t in types.Where(p => !p.IsInterface && !p.IsAbstract && p.GetConstructor(new Type[0]) != null))
            {
                if (type == null || type.IsAssignableFrom(t))
                    type = t;
            }

            if (type == null)
                return null;

            return Activator.CreateInstance(type);
        }
        public static System.Collections.IList GetListOf(Type type)
        {
            Type lst = typeof(List<>).MakeGenericType(type);
            return (System.Collections.IList)Activator.CreateInstance(lst);
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

        public static DateTime TruncateDateToTimeslice(this DateTime dateTime, TimeSpan timeSpan)
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
        public static bool IsTimeSpan(this object ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return IsTimeSpan(ext.GetType());
        }
        public static bool IsTimeSpan(Type type)
        {
            type = type ?? throw new ArgumentNullException(nameof(Type));
            return type == typeof(TimeSpan) || type == typeof(TimeSpan?);
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

        public static bool IsEnumerable(this object ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return IsEnumerable(ext.GetType());
        }
        public static bool IsEnumerable(Type type)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));
            return type.Name == "IEnumerable`1";
        }

        public static bool HasEnumerable(Type type)
        {
            return TypeExtensions.Configuration.GetInterfaces(type).Any(p => IsEnumerable(p));
        }

        public static bool CheckObjectInclusion(this object obj, object included)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (included == null)
                return false;

            Type type = obj.GetType();
            foreach (PropertyInfo pinfo in included.GetType().GetProperties().Where(p => p.CanRead))
            {
                PropertyInfo tmp = type.GetProperty(pinfo.Name);
                if (
                    tmp == null ||
                    pinfo.PropertyType != tmp.PropertyType ||
                    (Configuration.Get(pinfo, included) == null && Configuration.Get(tmp, obj) != null) ||
                    (Configuration.Get(pinfo, included) != null && Configuration.Get(tmp, obj) == null) ||
                    (Configuration.Get(pinfo, included) != null && Configuration.Get(tmp, obj) != null && !Configuration.Get(pinfo, included).Equals(ObjectExtensions.Configuration.Get(tmp, obj))))
                    return false;
            }
            return true;
        }
        
        public static Type GetElementType(Type seqType)
        {
            Type ienum = FindIEnumerable(seqType);
            if (ienum == null) return seqType;
            return ienum.GetGenericArguments()[0];
        }
        public static Type FindIEnumerable(Type seqType)
        {
            if (seqType == null || seqType == typeof(string))
                return null;

            if (seqType.IsArray)
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());

            if (seqType.IsGenericType)
            {
                foreach (Type arg in seqType.GetGenericArguments())
                {
                    Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(seqType))
                    {
                        return ienum;
                    }
                }
            }

            Type[] ifaces = TypeExtensions.Configuration.GetInterfaces(seqType).ToArray();
            if (ifaces != null && ifaces.Length > 0)
            {
                foreach (Type iface in ifaces)
                {
                    Type ienum = FindIEnumerable(iface);
                    if (ienum != null) return ienum;
                }
            }

            Type baseType = seqType.BaseType;
            if (baseType != null && baseType != typeof(object))
            {
                return FindIEnumerable(baseType);
            }

            return null;
        }
    }
}
