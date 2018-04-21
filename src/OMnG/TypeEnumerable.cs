using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OMnG
{
    public sealed class TypeEnumerable : IEnumerable<TypeHolder>
    {
        public Type Type { get; private set; }
        private List<TypeHolder> _types;
        internal TypeEnumerable(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
        public IEnumerator<TypeHolder> GetEnumerator()
        {
            if(_types == null)
            {
                _types = new List<TypeHolder>() { new TypeHolder(Type) };
                _types.AddRange(TypeExtensions.Configuration.GetInterfaces(Type).Select(p=>new TypeHolder(p)));
                Type tmp = Type.BaseType;
                while(tmp!=null)
                {
                    _types.Add(new TypeHolder(tmp));
                    tmp = tmp.BaseType;
                }
            }

            return new TypeEnumerator(_types);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public sealed class TypeEnumerator : IEnumerator<TypeHolder>
    {
        public List<TypeHolder> Types { get; private set; }
        private int _currentIndex = -1;
        private bool _disposed = false;
        public TypeEnumerator(List<TypeHolder> types)
        {
            Types = types ?? throw new ArgumentNullException(nameof(types));
        }
        public TypeHolder Current
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException("TypeEnumerable");
                return _currentIndex < 0 ? null : Types[_currentIndex];
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _disposed = true;
            Types.Clear();
            Types = null;
        }

        public bool MoveNext()
        {
            if (_disposed)
                throw new ObjectDisposedException("TypeEnumerable");
            return ++_currentIndex < Types.Count;
        }

        public void Reset()
        {
            if (_disposed)
                throw new ObjectDisposedException("TypeEnumerable");
            _currentIndex = -1;
        }
    }
}
