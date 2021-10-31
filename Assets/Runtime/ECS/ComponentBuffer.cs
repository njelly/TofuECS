using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Tofunaut.TofuECS
{
    internal unsafe class ComponentBuffer<TComponent> : IComponentBuffer where TComponent : unmanaged
    {
        public int NumInUse => _length - _freeIndexes.Count;

        private TComponent* _buffer;
        private readonly Queue<int> _freeIndexes;
        private int _length;

        public ComponentBuffer()
        {
            _length = 1;
            _buffer = (TComponent*)Marshal.AllocHGlobal(Marshal.SizeOf<TComponent>());
            _freeIndexes = new Queue<int>();
            _freeIndexes.Enqueue(0);
        }

        public int Request()
        {
            int UseNextFreeIndex()
            {
                var freeIndex = _freeIndexes.Dequeue();
                _buffer[freeIndex] = new TComponent();
                return freeIndex;
            }

            if (_freeIndexes.Count > 0)
                return UseNextFreeIndex();

            // expand the buffer - there are no free indexes
            var prevLength = _length;
            var prevByteCount = Marshal.SizeOf<TComponent>() * prevLength;
            _length *= 2;
            var newBuffer = (TComponent*)Marshal.AllocHGlobal(Marshal.SizeOf<TComponent>() * _length);
            Buffer.MemoryCopy(_buffer, newBuffer, prevByteCount, prevByteCount);
            Marshal.FreeHGlobal((IntPtr)_buffer);
            _buffer = newBuffer;

            // fill the next free index queue with all the newly created indexes
            for (var i = prevLength; i < _length; i++)
                _freeIndexes.Enqueue(i);

            return UseNextFreeIndex();
        }

        public void Release(int index) => _freeIndexes.Enqueue(index);

        public TComponent Get(int index) => _buffer[index];

        public TComponent* GetUnsafe(int index) => &_buffer[index];

        public void Recycle(IComponentBuffer other)
        {
            var otherBuffer = (ComponentBuffer<TComponent>)other;

            if (_length < otherBuffer._length)
            {
                Marshal.FreeHGlobal((IntPtr)_buffer);
                _buffer = (TComponent*)Marshal.AllocHGlobal(Marshal.SizeOf<TComponent>() * otherBuffer._length);
                _length = otherBuffer._length;
            }

            var byteLength = Marshal.SizeOf<TComponent>() * _length;
            Buffer.MemoryCopy(otherBuffer._buffer, _buffer, byteLength, byteLength);

            _freeIndexes.Clear();

            foreach (var freeIndex in otherBuffer._freeIndexes)
                _freeIndexes.Enqueue(freeIndex);
        }
    }
}