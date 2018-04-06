using System;
using System.Collections.Generic;
using System.Text;

namespace OMnG
{
    public class CustomDisposable : IDisposable
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
