using System;

namespace WebAtoms
{
    public class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable instance = new EmptyDisposable();

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

}