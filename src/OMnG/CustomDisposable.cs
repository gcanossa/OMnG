using System;
using System.Collections.Generic;
using System.Text;

namespace OMnG
{
    internal class CustomDisposable : IDisposable
    {
        private Action _dispose;
        public CustomDisposable(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            _dispose?.Invoke();
        }
    }
}
