using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OMnG
{
    public static class TypeExtensions
    {
        private static TypeExtensionsConfiguration DefaultConfiguration = new TypeExtensionsConfiguration.DefaultConfiguration();

        [ThreadStatic]
        internal static Stack<TypeExtensionsConfiguration> _configuration;
        public static TypeExtensionsConfiguration Configuration
        {
            get
            {
                _configuration = _configuration ?? new Stack<TypeExtensionsConfiguration>();
                if (_configuration.Count == 0)
                    _configuration.Push(DefaultConfiguration);
                return _configuration.Peek();
            }
        }
        public static IDisposable ConfigScope(TypeExtensionsConfiguration configuration)
        {
            configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _configuration = _configuration ?? new Stack<TypeExtensionsConfiguration>();

            _configuration.Push(configuration);

            return new CustomDisposable(() => _configuration.Pop());
        }
        public static object Scope<T>(this T ext, TypeExtensionsConfiguration configuration, Func<T, object> action)
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
            
            if (!Configuration.FilterValidType(type))
                throw new ArgumentException($"The type is configured to be unusable", nameof(type));

            return Configuration.ToLabel(type);
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

            foreach (Type item in Configuration.GetInterfaces(type).Where(Configuration.FilterValidType))
            {
                result.Add(GetLabel(item));
            }

            while (type != typeof(object) && !type.IsInterface)
            {
                type = type.BaseType;
                if (type != typeof(object) && Configuration.FilterValidType(type))
                    result.Add(GetLabel(type));
            }

            return result;
        }

        public static IEnumerable<Type> GetTypesFromLabels(this IEnumerable<string> labels)
        {
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));
            
            return labels.Select(p => Configuration.ToType(p));
        }
    }
}
