using System;
using System.Collections.Generic;
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

            var rawValue = RawValue;
            for (var i = 0; i < length; i++)
                rawValue[i] = default;
        }

        public UnmanagedArray(IReadOnlyList<T> arr)
        {
            Length = arr.Count;
            IsDisposed = false;
            _ptr = Marshal.AllocHGlobal(sizeof(T) * Length);

            var rawValue = RawValue;
            for (var i = 0; i < Length; i++)
                rawValue[i] = arr[i];
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