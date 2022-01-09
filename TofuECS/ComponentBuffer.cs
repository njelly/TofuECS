using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    internal class ComponentBuffer<TComponent> where TComponent : unmanaged
    {
        
        /// <summary>
        /// ComponentBuffers can act as linked lists when allowed to arbitrarily expand to fit data.
        /// </summary>
        internal ComponentBuffer<TComponent> NextBuffer { get; private set; }

        private readonly TComponent[] _buffer;
        private readonly int[] _entityAssignments;
        private readonly Queue<int> _freeIndexes;
        private readonly bool _canExpand;
        private int _currentIteratorIndex;

        public ComponentBuffer(int size, bool canExpand)
        {
            _buffer = new TComponent[size];
            _entityAssignments = new int[size];
            _freeIndexes = new Queue<int>(size);
            _canExpand = canExpand;

            for (var i = size - 1; i >= 0; i--)
            {
                _entityAssignments[i] = ECS.InvalidEntityId;
                _freeIndexes.Enqueue(i);
            }
        }

        public int Request(int entityId, TComponent component)
        {
            if (_freeIndexes.Count > 0)
            {
                if (!_canExpand)
                    throw new BufferFullException<TComponent>();
                
                var nextIndex = _freeIndexes.Dequeue();
                _entityAssignments[nextIndex] = entityId;
                _buffer[nextIndex] = component;
                return nextIndex;
            }

            if (NextBuffer == null)
                NextBuffer = new ComponentBuffer<TComponent>(_buffer.Length, _canExpand);

            return NextBuffer.Request(entityId, component) + _buffer.Length;
        }

        public void Release(int componentIndex)
        {
            if (componentIndex >= _buffer.Length)
            {
                if (NextBuffer == null)
                    throw new InvalidComponentIndexException<TComponent>(componentIndex);

                NextBuffer.Release(componentIndex - _buffer.Length);
                return;
            }
            
            _freeIndexes.Enqueue(componentIndex);
            _entityAssignments[componentIndex] = ECS.InvalidEntityId;
        }

        public void ResetIterator() => _currentIteratorIndex = 0;

        public bool Next(out int entityId, out TComponent component)
        {
            if (_currentIteratorIndex < _buffer.Length)
            {
                entityId = _entityAssignments[_currentIteratorIndex];
                component = _buffer[_currentIteratorIndex];
                _currentIteratorIndex++;
                return true;
            }

            entityId = ECS.InvalidEntityId;
            component = default;
            return false;
        }
        
        public unsafe bool NextUnsafe(out int entityId, out TComponent* component)
        {
            if (_currentIteratorIndex < _buffer.Length)
            {
                fixed (TComponent* ptr = &_buffer[_currentIteratorIndex])
                {
                    entityId = _entityAssignments[_currentIteratorIndex];
                    component = ptr;
                    _currentIteratorIndex++;
                    return true;
                }
            }

            entityId = ECS.InvalidEntityId;
            component = default;
            return false;
        }

        public TComponent Get(int componentIndex)
        {
            if (componentIndex < _buffer.Length) 
                return _buffer[componentIndex];
            
            if (NextBuffer == null)
                throw new InvalidComponentIndexException<TComponent>(componentIndex);

            return NextBuffer.Get(componentIndex - _buffer.Length);
        }

        public unsafe TComponent* GetUnsafe(int componentIndex)
        {
            if (componentIndex < _buffer.Length)
            {
                fixed (TComponent* ptr = &_buffer[componentIndex])
                    return ptr;
            }
            
            if (NextBuffer == null)
                throw new InvalidComponentIndexException<TComponent>(componentIndex);

            return NextBuffer.GetUnsafe(componentIndex - _buffer.Length);
        }
    }

    public class BufferFullException<TComponent> : Exception where TComponent : unmanaged
    {
        public override string Message =>
            $"Unable to complete the operation, the buffer of type {typeof(TComponent)} is full.";
    }

    public class InvalidComponentIndexException<TComponent> : Exception where TComponent : unmanaged
    {
        public override string Message =>
            $"The component index {_index} for buffer type {typeof(TComponent)} is invalid.";
        private readonly int _index;
        public InvalidComponentIndexException(int index)
        {
            _index = index;
        }
    }
}