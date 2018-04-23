using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OMnG
{
    public static class TypeQueriesExtensions
    {
        public static IEnumerable<TypeHolder> AsTypeEnumerable(this object ext)
        {
            return AsTypeEnumerable(ext?.GetType());
        }
        public static IEnumerable<TypeHolder> AsTypeEnumerable(this Type ext)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            return new TypeEnumerable(ext);
        }

        public static IEnumerable<TypeHolder[]> GetGenericArgumentsOf(this Type ext, Type genericTypeDefinition)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            genericTypeDefinition = genericTypeDefinition ?? throw new ArgumentNullException(nameof(genericTypeDefinition));

            if (genericTypeDefinition.IsGenericTypeDefinition == false)
                throw new ArgumentException("Must be a generic type definition", nameof(genericTypeDefinition));

            return ext.AsTypeEnumerable()
                .Where(p => p.Type.IsGenericType && p.Type.GetGenericTypeDefinition() == genericTypeDefinition)
                .Select(p => p.Type.GetGenericArguments().Select(t => new TypeHolder(t)).ToArray());
        }

        public static bool IsOfGenericType(this Type ext, Type genericTypeDefinition, params Func<TypeHolder, bool>[] genericArgumentsConstraints)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            genericTypeDefinition = genericTypeDefinition ?? throw new ArgumentNullException(nameof(genericTypeDefinition));

            if (genericTypeDefinition.IsGenericTypeDefinition == false)
                throw new ArgumentException("Must be a generic type definition", nameof(genericTypeDefinition));

            if(genericArgumentsConstraints!=null && genericArgumentsConstraints.Length>0)
            {
                return ext.GetGenericArgumentsOf(genericTypeDefinition).Where(p=> 
                {
                    for (int i = 0; i < p.Length && i < genericArgumentsConstraints.Length; i++)
                    {
                        if (!genericArgumentsConstraints[i](p[i]))
                            return false;
                    }
                    return true;
                }).Any();
            }
            else
                return ext.GetGenericArgumentsOf(genericTypeDefinition).Any();
        }
        
        public static bool IsEnumerable(this Type ext)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            return ext.IsOfGenericType(typeof(IEnumerable<>));
        }
        public static bool IsEnumerableOfAssignableTypes(this Type ext, Type referenceType)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            referenceType = referenceType ?? throw new ArgumentNullException(nameof(referenceType));

            return ext.IsOfGenericType(typeof(IEnumerable<>), p => referenceType <= p);
        }

        public static bool IsCollection(this Type ext)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            return ext.IsOfGenericType(typeof(ICollection<>));
        }
        public static bool IsCollectionOfAssignableTypes(this Type ext, Type referenceType)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            referenceType = referenceType ?? throw new ArgumentNullException(nameof(referenceType));

            return ext.IsOfGenericType(typeof(ICollection<>), p => referenceType <= p);
        }

        public static bool IsConvertibleTo(this Type ext, Type referenceType)
        {
            TypeHolder extHolder =  ext ?? throw new ArgumentNullException(nameof(ext));
            referenceType = referenceType ?? throw new ArgumentNullException(nameof(referenceType));
            
            if ((referenceType.IsValueType || referenceType == typeof(string)) && (ext.IsValueType || ext == typeof(string)))
                return true;
            else if (referenceType <= extHolder)
                return true;
            else if (referenceType.IsGenericType)
            {
                List<Func<TypeHolder, bool>> constranints = new List<Func<TypeHolder, bool>>();
                foreach (TypeHolder item in referenceType.GetGenericArgumentsOf(referenceType.GetGenericTypeDefinition()).First())
                {
                    constranints.Add(p => p.Type.IsConvertibleTo(item.Type));
                }
                return ext.IsOfGenericType(referenceType.GetGenericTypeDefinition(), constranints.ToArray());
            }
            return false;
        }

        public static object ConvertTo<T>(this object ext)
        {
            return ConvertTo(ext, typeof(T));
        }
        public static object ConvertTo(this object ext, Type toType)
        {
            if (ext == null)
                return toType.GetDefault();
            else if (toType == typeof(string))
                return ext.ToString();
            else if (ext.GetType().IsConvertibleTo(toType))
            {
                if (toType.IsEnum)
                    return Enum.Parse(toType, (string)ext.ConvertTo<string>());
                else if (toType.IsValueType)
                    return Convert.ChangeType(ext, toType.GetGenericArgumentsOf(typeof(Nullable<>)).FirstOrDefault()?.FirstOrDefault()?.Type??toType);
                else if ((TypeHolder)toType < ext.GetType())
                    return ext;

                TypeHolder[] types = ext.GetType()
                    .GetGenericArgumentsOf(typeof(IEnumerable<>))
                    .Where(p => p.Any(t => t.Type.IsConvertibleTo(toType
                    .GetGenericArguments()[0])))
                    .FirstOrDefault();

                if (types != null)
                {
                    if (types[0].Type.IsOfGenericType(typeof(KeyValuePair<,>)))
                    {
                        Type[] kvTypes = toType.GetGenericArgumentsOf(typeof(IEnumerable<>)).First()[0].Type
                            .GetGenericArgumentsOf(typeof(KeyValuePair<,>)).First().Select(p=>p.Type).ToArray();

                        IDictionary res = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(kvTypes));
                        foreach (object item in ext as IEnumerable)
                        {
                            res.Add(
                                ObjectExtensions.Configuration.Get(types[0].Type.GetProperty("Key"), item).ConvertTo(kvTypes[0]),
                                ObjectExtensions.Configuration.Get(types[0].Type.GetProperty("Value"),item).ConvertTo(kvTypes[1])
                                );
                        }
                        return res;
                    }
                    else
                    {
                        IList res = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(types.Select(p => p.Type).ToArray()));
                        foreach (object item in ext as IEnumerable)
                        {
                            res.Add(item.ConvertTo(types[0].Type));
                        }
                        return res;
                    }
                }
                else
                    throw new InvalidCastException($"Unable to convert object of type {ext.GetType().FullName} to type {toType.FullName}");
            }
            else
                throw new InvalidCastException($"Unable to convert object of type {ext.GetType().FullName} to type {toType.FullName}");
        }

        public static object GetDefault(this Type type)
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
        
        public static object GetInstanceOf(this Type type, IDictionary<string, object> param)
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
        public static bool IsDateTime(this Type type)
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
        public static bool IsTimeSpan(this Type type)
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
        public static bool IsNumeric(this Type type)
        {
            type = type ?? throw new ArgumentNullException(nameof(Type));
            return type.IsPrimitive && type != typeof(char) && type != typeof(bool);
        }

        public static bool IsPrimitive(this object ext)
        {
            if (ext == null)
                throw new ArgumentNullException(nameof(ext));

            return IsPrimitive(ext.GetType());
        }
        public static bool IsPrimitive(this Type type)
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

        public static bool DoesInclude(this object obj, object included)
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
                    (ObjectExtensions.Configuration.Get(pinfo, included) == null && ObjectExtensions.Configuration.Get(tmp, obj) != null) ||
                    (ObjectExtensions.Configuration.Get(pinfo, included) != null && ObjectExtensions.Configuration.Get(tmp, obj) == null) ||
                    (ObjectExtensions.Configuration.Get(pinfo, included) != null && ObjectExtensions.Configuration.Get(tmp, obj) != null && !ObjectExtensions.Configuration.Get(pinfo, included).Equals(ObjectExtensions.Configuration.Get(tmp, obj))))
                    return false;
            }
            return true;
        }
    }
}
