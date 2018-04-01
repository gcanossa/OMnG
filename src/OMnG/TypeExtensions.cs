using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OMnG
{
    public static class TypeExtensions
    {
        private static object _lk = new object();
        private static TypeExtensionsConfiguration _configuration = new TypeExtensionsConfiguration.DefaultConfiguration();
        public static TypeExtensionsConfiguration Configuration { get { lock (_lk) { return _configuration; } } set { lock (_lk) { _configuration = value; } } }
        
        public static string GetLablel(this object obj)
        {
            return GetLabel(obj?.GetType());
        }
        public static string GetLabel<T>()
        {
            return GetLabel(typeof(T));
        }
        public static string GetLabel(this Type type, TypeExtensionsConfiguration configuration = null)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));

            configuration = configuration ?? Configuration;

            if (!configuration.FilterValidType(type))
                throw new ArgumentException($"The type is configured to be unusable", nameof(type));

            return configuration.ToLabel(type);
        }

        public static IEnumerable<string> GetLabels(this object obj)
        {
            return GetLabels(obj?.GetType());
        }
        public static IEnumerable<string> GetLabels<T>()
        {
            return GetLabels(typeof(T));
        }
        public static IEnumerable<string> GetLabels(this Type type, TypeExtensionsConfiguration configuration = null)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));

            configuration = configuration ?? Configuration;

            HashSet<string> result = new HashSet<string>();
            result.Add(GetLabel(type));

            foreach (Type item in configuration.GetInterfaces(type).Where(configuration.FilterValidType))
            {
                result.Add(GetLabel(item));
            }

            while (type != typeof(object) && !type.IsInterface)
            {
                type = type.BaseType;
                if (type != typeof(object) && configuration.FilterValidType(type))
                    result.Add(GetLabel(type));
            }

            return result;
        }

        public static IEnumerable<Type> GetTypesFromLabels(this IEnumerable<string> labels, TypeExtensionsConfiguration configuration = null)
        {
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));

            configuration = configuration ?? Configuration;

            return labels.Select(p => configuration.ToType(p));
        }
    }
}
