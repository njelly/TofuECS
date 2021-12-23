using System;
using System.Runtime.InteropServices;

namespace Tofunaut.TofuECS.Utilities
{
    /// <summary>
    /// A helper class for unmanaged arrays, useful when components need to have arrays.
    /// </summary>
    public unsafe struct UnmanagedArray<T> : IDisposable where T : unmanaged
    {
        public int Length { get; }
        public bool IsDisposed { get; private set; }
        
        /// <summary>
        /// This is faster when iterating over the values in the array, since each call to this[index] requires a cast.
        /// </summary>
        public T* RawValue => (T*)_ptr.ToPointer();

        public T this[int index]
        {
            get => RawValue[index];
            set => RawValue[index] = value;
        }
        
        private IntPtr _ptr;

        public UnmanagedArray(int length)
        {
            Length = length;
            IsDisposed = false;
            _ptr = Marshal.AllocHGlobal(sizeof(T) * Length);

            for (var i = 0; i < length; i++)
                this[i] = default;
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            
            Marshal.FreeHGlobal(_ptr);
            IsDisposed = true;
        }
    }
}