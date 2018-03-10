using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OMnG
{
    public abstract class TypeExtensionsConfiguration
    {
        #region nested types

        public class DefaultConfiguration : TypeExtensionsConfiguration
        {
            public override string ToLabel(Type type)
            {
                return $"_{type.FullName.Replace(".", "$").Replace("+", "$$")}";
            }

            public override Type ToType(string label)
            {
                return AllTypes().First(p => MatchType(p, label.Substring(1).Replace("$$", "+").Replace("$", ".")));
            }

            public override bool MatchType(Type type, string label)
            {
                return type.FullName == label;
            }
        }

        public class CompressConfiguration : DefaultConfiguration
        {
            private Dictionary<string, string> _hashToName = new Dictionary<string, string>();
            
            public override string ToLabel(Type type)
            {
                string s = base.ToLabel(type);
                string c = s;
                if (s.Length > 32)
                {
                    using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
                    {
                        byte[] originalBytes = ASCIIEncoding.Default.GetBytes(s);
                        byte[] encodedBytes = md5.ComputeHash(originalBytes);

                        c = BitConverter.ToString(encodedBytes).Replace("-", "");

                        _hashToName.Add(c, s);
                    }
                }

                return c;
            }

            public override Type ToType(string label)
            {
                return AllTypes().First(p => MatchType(p, _hashToName[label].Substring(1).Replace("$$", "+").Replace("$", ".")));
            }
        }

        #endregion

        public abstract string ToLabel(Type type);
        public abstract Type ToType(string label);

        public abstract bool MatchType(Type type, string label);

        public virtual bool FilterValidType(Type type)
        {
            return !type.IsGenericType;
        }

        protected IEnumerable<Type> AllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(p => p.GetTypes());
        }

        protected void Validate()
        {
            if (ToType(ToLabel(typeof(TypeExtensionsConfiguration.DefaultConfiguration))) != typeof(TypeExtensionsConfiguration.DefaultConfiguration))
                throw new InvalidOperationException($"{nameof(ToType)} is not the inverse of {nameof(ToLabel)}");
        }

        public TypeExtensionsConfiguration()
        {
            Validate();
        }
    }
}
