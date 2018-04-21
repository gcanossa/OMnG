using System;
using System.Collections.Generic;
using System.Text;

namespace OMnG
{
    public sealed class TypeHolder
    {
        public Type Type { get; private set; }
        internal TypeHolder(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public static explicit operator Type(TypeHolder holder)
        {
            return holder?.Type;
        }
        public static implicit operator TypeHolder(Type type)
        {
            return type == null ? null : new TypeHolder(type);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;
            else
                return Type.Equals(((TypeHolder)obj).Type);
        }
        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }
        public static bool operator ==(TypeHolder first, TypeHolder second)
        {
            return first?.Type == second?.Type;
        }
        public static bool operator !=(TypeHolder first, TypeHolder second)
        {
            return !(first == second);
        }
        public static bool operator <(TypeHolder first, TypeHolder second)
        {
            return first.Type.IsAssignableFrom(second.Type) && first!=second;
        }
        public static bool operator >(TypeHolder first, TypeHolder second)
        {
            return second < first;
        }
        public static bool operator <=(TypeHolder first, TypeHolder second)
        {
            return first.Type.IsAssignableFrom(second.Type);
        }
        public static bool operator >=(TypeHolder first, TypeHolder second)
        {
            return second <= first;
        }
    }
}
