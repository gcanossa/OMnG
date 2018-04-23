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
        #region scoping
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
            _configuration = _configuration ?? new Stack<ObjectExtensionsConfiguration>();

            _configuration.Push(configuration);

            return new CustomDisposable(()=>_configuration.Pop());
        }

        public static object Scope<T>(this T ext, ObjectExtensionsConfiguration configuration, Func<T, object> action)
            where T : class
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            action = action ?? throw new ArgumentNullException(nameof(action));

            using (ConfigScope(configuration))
            {
                return action(ext);
            }
        }

        #endregion

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

        #region ExcludeProperties

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

        #endregion

        #region specific ExcludeProperties
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
        public static IDictionary<string, object> ExludeMatchingTypesProperties<T>(this T ext, Func<Type, bool> filter) where T : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            filter = filter ?? throw new ArgumentNullException(nameof(filter));

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string item in typeof(T).GetProperties()
                .Where(p => !filter(p.PropertyType))
                .Select(p => p.Name))
            {
                result.Add(item, ext.GetPropValue(item));
            }

            return result;
        }
        public static IDictionary<string, object> ExludeMatchingTypesProperties(this IDictionary<string, object> ext, Func<Type, bool> filter)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            filter = filter ?? throw new ArgumentNullException(nameof(filter));

            return ext
                .Where(p => p.Value == null || !filter(p.Value.GetType())).ToDictionary(p => p.Key, p => p.Value);
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
        #endregion

        #region SelectProperties

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

        #endregion

        #region specific SelectProperties
        public static IDictionary<string, object> SelectPrimitiveTypesProperties<T>(this T ext) where T : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string item in typeof(T).GetProperties()
                .Where(p => p.PropertyType.IsPrimitive())
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
                .Where(p => p.Value == null || p.Value.IsPrimitive()).ToDictionary(p=>p.Key, p=>p.Value);
        }
        public static IDictionary<string, object> SelectMatchingTypesProperties<T>(this T ext, Func<Type, bool> filter) where T : class
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            filter = filter ?? throw new ArgumentNullException(nameof(filter));

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (string item in typeof(T).GetProperties()
                .Where(p => filter(p.PropertyType))
                .Select(p => p.Name))
            {
                result.Add(item, ext.GetPropValue(item));
            }

            return result;
        }
        public static IDictionary<string, object> SelectMatchingTypesProperties(this IDictionary<string, object> ext, Func<Type, bool> filter)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));
            filter = filter ?? throw new ArgumentNullException(nameof(filter));

            return ext
                .Where(p => p.Value==null || filter(p.Value.GetType())).ToDictionary(p => p.Key, p => p.Value);
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
        #endregion

        #region ValueCopying
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
        #endregion

    }
}
