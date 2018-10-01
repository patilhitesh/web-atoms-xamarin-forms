using System;

namespace WebAtoms
{
    public class AtomDisposable : IDisposable
    {
        readonly Action action;

        public AtomDisposable(Action action)
        {
            this.action = action;
        }

        public void Dispose()
        {
            action?.Invoke();
        }
    }

}