using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OMnG
{
    public static class TypeExtensions
    {
        public static string EscapeName(this string name)
        {
            return name?.Replace(".","_").Replace("+","__");
        }
        public static string UnescapeName(this string name)
        {
            return name?.Replace("__", "+").Replace("_", ".");
        }

        public static string GetLablel(this object obj)
        {
            return GetLabel(obj?.GetType());
        }
        public static string GetLabel<T>()
        {
            return GetLabel(typeof(T));
        }
        public static string GetLabel(this Type type)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));

            return type.FullName.EscapeName();
        }

        public static IEnumerable<string> GetLabels(this object obj)
        {
            return GetLabels(obj?.GetType());
        }
        public static IEnumerable<string> GetLabels<T>()
        {
            return GetLabels(typeof(T));
        }
        public static IEnumerable<string> GetLabels(this Type type)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));

            HashSet<string> result = new HashSet<string>();
            result.Add(GetLabel(type));

            foreach (Type item in type.GetInterfaces())
            {
                result.Add(GetLabel(item));
            }

            while (type != typeof(object) && !type.IsInterface)
            {
                type = type.BaseType;
                if (type != typeof(object))
                    result.Add(GetLabel(type));
            }

            return result;
        }

        public static IEnumerable<Type> GetTypesFromLabels(this IEnumerable<string> labels)
        {
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));

            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(p => p.GetTypes());

            return labels.Select(p => types.FirstOrDefault(x => x.FullName.EscapeName() == p));
        }
        
        public static object GetInstanceOfMostSpecific(this IEnumerable<Type> types)
        {
            types = types ?? throw new ArgumentNullException(nameof(types));

            Type type = null;
            foreach (Type t in types.Where(p=>!p.IsInterface && !p.IsAbstract && p.GetConstructor(new Type[0])!=null))
            {
                if (type == null || type.IsAssignableFrom(t))
                    type = t;
            }

            if (type == null)
                return null;

            return Activator.CreateInstance(type);
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
                    (pinfo.GetValue(included) == null && tmp.GetValue(obj) != null) ||
                    (pinfo.GetValue(included) != null && tmp.GetValue(obj) == null) ||
                    (pinfo.GetValue(included) != null && tmp.GetValue(obj) != null && !pinfo.GetValue(included).Equals(tmp.GetValue(obj))))
                    return false;
            }
            return true;
        }
    }
}
