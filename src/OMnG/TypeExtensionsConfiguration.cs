using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace OMnG
{
    public abstract class TypeExtensionsConfiguration
    {
        #region nested types

        public class DefaultConfiguration : TypeExtensionsConfiguration
        {
            protected override string GetLabel(Type type)
            {
                return $"{type.FullName}";
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

            protected override string GetLabel(Type type)
            {
                string s = base.GetLabel(type);
                return AddAndGet(s);
            }
        }

        #endregion

        public virtual string ToLabel(Type type)
        {
            ImportNewAssemblies();

            if (!LabelTypes.ContainsKey(type))
                ManageType(GetLabel(type), type);

            return LabelTypes[type];
        }
        public virtual Type ToType(string label)
        {
            ImportNewAssemblies();

            return TypeLabels[label];
        }

        protected abstract string GetLabel(Type type);
        
        public virtual bool FilterValidType(Type type)
        {
            return !type.IsGenericType;
        }

        private List<Assembly> LoadedAssemblies = new List<Assembly>();
        private List<Type> LoadedTypes = new List<Type>();
        private Dictionary<Type, Type[]> TypeInterfaces = new Dictionary<Type, Type[]>();
        private Dictionary<string, Type> TypeLabels = new Dictionary<string, Type>();
        private Dictionary<Type, string> LabelTypes = new Dictionary<Type, string>();

        private void ImportNewAssemblies()
        {
            lock (LoadedAssemblies)
            {
                IEnumerable<Assembly> newAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(AssemblyLoadFilter).Except(LoadedAssemblies);

                if (newAssemblies.Count() > 0)
                {
                    LoadedTypes.AddRange(newAssemblies.SelectMany(p => p.GetTypes()).Distinct());

                    foreach (Type item in newAssemblies.SelectMany(p => p.GetTypes()).Distinct())
                    {
                        if(!TypeInterfaces.ContainsKey(item))
                            TypeInterfaces.Add(item, item.GetInterfaces());
                        ManageType(GetLabel(item), item);
                    }

                    LoadedAssemblies.AddRange(newAssemblies);
                }
            }
        }
        
        protected virtual void ManageType(string label, Type type)
        {
            if (!TypeLabels.ContainsKey(label))
            {
                AddType(label, type);
            }
        }

        protected void AddType(string label, Type type)
        {
            LabelTypes.Add(type, label);
            TypeLabels.Add(label, type);
        }
        protected virtual bool AssemblyLoadFilter(Assembly assembly)
        {
            return true;
        }
        protected IEnumerable<Type> AllTypes()
        {
            ImportNewAssemblies();

            return LoadedTypes;
        }

        public IEnumerable<Type> GetInterfaces(Type type)
        {
            ImportNewAssemblies();

            if(!TypeInterfaces.ContainsKey(type))
                TypeInterfaces.Add(type, type.GetInterfaces());

            return TypeInterfaces[type];
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
