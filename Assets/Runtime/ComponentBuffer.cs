using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    internal unsafe class ComponentBuffer<TComponent> : IComponentBuffer where TComponent : unmanaged
    {
        public int NumInUse => _components.Length - _freeIndexes.Count;

        private TComponent[] _components;
        private readonly Queue<int> _freeIndexes;

        public ComponentBuffer()
        {
            _components = new TComponent[1];
            _freeIndexes = new Queue<int>();
            _freeIndexes.Enqueue(0);
        }

        public int Request()
        {
            int UseNextFreeIndex()
            {
                var freeIndex = _freeIndexes.Dequeue();
                _components[freeIndex] = new TComponent();
                return freeIndex;
            }

            if (_freeIndexes.Count > 0)
                return UseNextFreeIndex();

            // expand the buffer - there are no free indexes
            var prevLength = _components.Length;
            var newBuffer = new TComponent[prevLength * 2];
            Array.Copy(_components, 0, newBuffer, 0, prevLength);
            _components = newBuffer;

            // fill the next free index queue with all the newly created indexes
            for (var i = prevLength; i < _components.Length; i++)
                _freeIndexes.Enqueue(i);

            return UseNextFreeIndex();
        }

        public void Release(int index) => _freeIndexes.Enqueue(index);

        public TComponent Get(int index) => _components[index];

        public TComponent* GetUnsafe(int index)
        {
            fixed (TComponent* ptr = &_components[index])
            {
                return ptr;
            }
        }

        public void CopyFrom(IComponentBuffer other)
        {
            var otherBuffer = (ComponentBuffer<TComponent>)other;

            if (_components.Length < otherBuffer._components.Length)
                _components = new TComponent[otherBuffer._components.Length];

            Array.Copy(otherBuffer._components, _components, otherBuffer._components.Length);

            _freeIndexes.Clear();

            foreach (var freeIndex in otherBuffer._freeIndexes)
                _freeIndexes.Enqueue(freeIndex);
        }
    }
}