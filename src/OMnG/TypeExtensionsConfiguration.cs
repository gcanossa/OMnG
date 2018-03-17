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
                return $"{type.FullName}";
            }

            public override Type ToType(string label)
            {
                return AllTypes().First(p => MatchType(p, label));
            }

            public override bool MatchType(Type type, string label)
            {
                return type.FullName == label;
            }
        }

        public class CompressConfiguration : DefaultConfiguration
        {
            private Dictionary<string, string> _hashToName = new Dictionary<string, string>();
            
            private string AddAndGet(string label)
            {
                string c = HashLabelstring(label);

                if (!_hashToName.ContainsKey(c))
                    _hashToName.Add(c, label);
                else if (_hashToName[c] != label)
                    throw new InvalidOperationException($"Duplicate hash found. '{label}' has the same hash of '{_hashToName[c]}' : '{c}'");

                return c;
            }

            private string HashLabelstring(string label)
            {
                if (label.Length > 32)
                {
                    using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
                    {
                        byte[] originalBytes = ASCIIEncoding.Default.GetBytes(label);
                        byte[] encodedBytes = md5.ComputeHash(originalBytes);

                        label = BitConverter.ToString(encodedBytes).Replace("-", "");
                    }
                }

                return label;
            }

            public override string ToLabel(Type type)
            {
                string s = base.ToLabel(type);
                return AddAndGet(s);
            }

            public override Type ToType(string label)
            {
                return AllTypes().First(p => MatchType(p, _hashToName.ContainsKey(label) ? _hashToName[label] : _hashToName[AddAndGet(label)]));
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
