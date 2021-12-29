using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Tofunaut.TofuECS
{
    internal unsafe class ComponentBuffer<TComponent> : IComponentBuffer where TComponent : unmanaged
    {
        private IntPtr _buffer;
        private readonly Queue<int> _freeIndexes;
        private int _length;

        public ComponentBuffer()
        {
            _length = 1;
            _buffer = Marshal.AllocHGlobal(Marshal.SizeOf<TComponent>());
            _freeIndexes = new Queue<int>();
            _freeIndexes.Enqueue(0);
        }

        public int Request()
        {
            int UseNextFreeIndex()
            {
                var freeIndex = _freeIndexes.Dequeue();
                ((TComponent*)_buffer.ToPointer())[freeIndex] = new TComponent();
                return freeIndex;
             }

            if (_freeIndexes.Count > 0)
                return UseNextFreeIndex();

            // expand the buffer - there are no free indexes
            var prevLength = _length;
            var prevByteCount = Marshal.SizeOf<TComponent>() * prevLength;
            _length *= 2;
            var newBuffer = Marshal.AllocHGlobal(Marshal.SizeOf<TComponent>() * _length);
            Buffer.MemoryCopy(_buffer.ToPointer(), newBuffer.ToPointer(), prevByteCount, prevByteCount);
            Marshal.FreeHGlobal(_buffer);
            _buffer = newBuffer;

            // fill the next free index queue with all the newly created indexes
            for (var i = prevLength; i < _length; i++)
                _freeIndexes.Enqueue(i);

            return UseNextFreeIndex();
        }

        public void Release(int index) => _freeIndexes.Enqueue(index);

        public TComponent Get(int index) => ((TComponent*)_buffer.ToPointer())[index];

        public TComponent* GetUnsafe(int index) => &((TComponent*)_buffer.ToPointer())[index];

        public void Recycle(IComponentBuffer other)
        {
            var otherBuffer = (ComponentBuffer<TComponent>)other;

            if (_length < otherBuffer._length)
            {
                Marshal.FreeHGlobal(_buffer);
                _buffer = Marshal.AllocHGlobal(Marshal.SizeOf<TComponent>() * otherBuffer._length);
                _length = otherBuffer._length;
            }

            var byteLength = Marshal.SizeOf<TComponent>() * _length;
            Buffer.MemoryCopy(otherBuffer._buffer.ToPointer(), _buffer.ToPointer(), byteLength, byteLength);

            _freeIndexes.Clear();

            foreach (var freeIndex in otherBuffer._freeIndexes)
                _freeIndexes.Enqueue(freeIndex);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(_buffer);
        }
    }
}