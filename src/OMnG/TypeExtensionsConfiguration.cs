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
            public override Func<Type, string> ToLabel => 
                t => $"{t.FullName.Replace(".", "$").Replace("+", "$$")}";
            
            public override Func<string, Type> ToType => 
                n => AllTypes.First(p=>MatchType(p,n.Replace("$$", "+").Replace("$", ".")));
            
            public override Func<Type, string, bool> MatchType => 
                (t, n) =>
                    t.FullName == n;
        }

        public class CompressConfiguration : DefaultConfiguration
        {
            private Dictionary<string, string> _hashToName = new Dictionary<string, string>();
            
            public override Func<Type, string> ToLabel => 
                t=>
                {
                    string s = base.ToLabel(t);
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
                };

            public override Func<string, Type> ToType =>
                n => AllTypes.First(p => MatchType(p, _hashToName[n].Replace("$$", "+").Replace("$", ".")));
        }

        #endregion

        public abstract Func<Type, string> ToLabel { get; }
        public abstract Func<string, Type> ToType { get; }

        public abstract Func<Type, string, bool> MatchType { get; }

        public virtual Func<Type, bool> FilterValidType { get; } =
            t => !t.IsGenericType;

        protected IEnumerable<Type> AllTypes => AppDomain.CurrentDomain.GetAssemblies().SelectMany(p => p.GetTypes());

        protected void Validate()
        {
            if (ToType(ToLabel(typeof(TypeExtensionsConfiguration.DefaultConfiguration))) != typeof(TypeExtensionsConfiguration.DefaultConfiguration))
                throw new InvalidOperationException($"{nameof(ToType)} is not the inverse of {nameof(ToLabel)}");
        }

        public TypeExtensionsConfiguration()
        {
            if (ToLabel == null)
                throw new ArgumentNullException(nameof(ToLabel));
            if (ToType == null)
                throw new ArgumentNullException(nameof(ToType));
            if (MatchType == null)
                throw new ArgumentNullException(nameof(MatchType));
            Validate();
        }
    }
}
