using System;
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

            if (referenceType <= extHolder)
                return true;
            else if(referenceType.IsGenericType)
            {
                List<Func<TypeHolder, bool>> constranints = new List<Func<TypeHolder, bool>>();
                foreach (TypeHolder item in referenceType.GetGenericArgumentsOf(referenceType.GetGenericTypeDefinition()).First())
                {
                    constranints.Add(p=>item < p);
                }
                return ext.IsOfGenericType(referenceType.GetGenericTypeDefinition(), constranints.ToArray());
            }
            return false;
        }
    }
}
