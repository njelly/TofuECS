using System;
using UnsafeCollections.Collections.Unsafe;

namespace Tofunaut.TofuECS
{
    public unsafe class AnonymousBuffer<TComponent> : IAnonymousComponentBuffer where TComponent : unmanaged
    {
        public int Size { get; }

        private readonly UnsafeArray* _arr;

        internal AnonymousBuffer(int size)
        {
            Size = size;
            _arr = UnsafeArray.Allocate<TComponent>(size);
        }

        internal AnonymousBuffer(TComponent[] components)
        {
            _arr = UnsafeArray.Allocate<TComponent>(components.Length);
            fixed (TComponent* componentsPtr = components)
                _arr->CopyFrom<TComponent>(componentsPtr, 0, components.Length);
        }

        public TComponent GetAt(int index) => UnsafeArray.Get<TComponent>(_arr, index);
        public TComponent* GetAtUnsafe(int index) => UnsafeArray.GetPtr<TComponent>(_arr, index);
        public void SetAt(int index, in TComponent component) => UnsafeArray.Set(_arr, index, component);

        public void GetState(TComponent[] state)
        {
            fixed (TComponent* componentsPtr = state)
                _arr->CopyTo<TComponent>(componentsPtr, 0);
        }

        public void SetState(TComponent[] state)
        {
            fixed (TComponent* componentsPtr = state)
                _arr->CopyFrom<TComponent>(componentsPtr, 0, state.Length);
        }
        
        /// <summary>
        /// Iterate over the buffer without creating garbage.
        /// </summary>
        /// <param name="i">The iterator to use.</param>
        /// <param name="component">The next valid component value.</param>
        /// <returns>Returns true as long as there exists another component.</returns>
        public bool Next(ref int i, out TComponent component)
        {
            if (i >= Size)
            {
                component = default;
                return false;
            }

            component = GetAt(i);
            i++;
            return true;
        }
        
        /// <summary>
        /// Iterate over the buffer without creating garbage (unsafe).
        /// </summary>
        /// <param name="i">The iterator to use.</param>
        /// <param name="component">A pointer to the next valid component.</param>
        /// <returns>Returns true as long as there exists another component.</returns>
        public bool NextUnsafe(ref int i, out TComponent* component)
        {
            if (i >= Size)
            {
                component = default;
                return false;
            }

            component = GetAtUnsafe(i);
            i++;
            return true;
        }

        public void Dispose()
        {
            UnsafeArray.Free(_arr);
        }
    }
}