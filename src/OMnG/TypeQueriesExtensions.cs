using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                return ObjectExtensions.GetDefault(toType);
            else if (toType == typeof(string))
                return ext.ToString();
            else if (ext.GetType().IsConvertibleTo(toType))
            {
                if (toType.IsEnum)
                    return Enum.Parse(toType, (string)ext.ConvertTo<string>());
                else if (toType.IsValueType)
                    return Convert.ChangeType(ext, toType);
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
                                types[0].Type.GetProperty("Key").GetValue(item).ConvertTo(kvTypes[0]),
                                types[0].Type.GetProperty("Value").GetValue(item).ConvertTo(kvTypes[1])
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
    }
}
