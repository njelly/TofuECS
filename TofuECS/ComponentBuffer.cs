using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public delegate void ModifyDelegate<TComponent>(ref TComponent component) where TComponent : unmanaged;
    
    public class ComponentBuffer<TComponent> where TComponent : unmanaged
    {
        public int Size => _buffer.Length;

        private TComponent[] _buffer;
        private int[] _entityAssignments;
        private readonly Queue<int> _freeIndexes;
        private readonly Dictionary<int, int> _entityIdToBufferIndex;

        internal ComponentBuffer(int size)
        {
            _buffer = new TComponent[size];
            _entityAssignments = new int[size];
            _freeIndexes = new Queue<int>(size);
            _entityIdToBufferIndex = new Dictionary<int, int>();

            for (var i = size - 1; i >= 0; i--)
            {
                _entityAssignments[i] = ECS.InvalidEntityId;
                _freeIndexes.Enqueue(i);
            }
        }

        public bool Get(int entityId, out TComponent component)
        {
            if (_entityIdToBufferIndex.TryGetValue(entityId, out var componentIndex))
            {
                component = _buffer[componentIndex];
                return true;
            }

            component = default;
            return false;
        }

        public bool GetAndModify(int entityId, ModifyDelegate<TComponent> modifyDelegate)
        {
            if (!_entityIdToBufferIndex.TryGetValue(entityId, out var componentIndex)) 
                return false;
            
            modifyDelegate.Invoke(ref _buffer[componentIndex]);
            return true;
        }

        public void Set(int entityId, in TComponent component = default)
        {
            if (_entityIdToBufferIndex.TryGetValue(entityId, out var componentIndex))
            {
                _buffer[componentIndex] = component;
                return;
            }
            
            if (_freeIndexes.Count <= 0)
                throw new BufferFullException<TComponent>();
            
            componentIndex = _freeIndexes.Dequeue();
            _entityIdToBufferIndex.Add(entityId, componentIndex);
            _entityAssignments[componentIndex] = entityId;
            _buffer[componentIndex] = component;
        }

        public bool Remove(int entityId)
        {
            if(!_entityIdToBufferIndex.TryGetValue(entityId, out var componentIndex))
                return false;
            
            _freeIndexes.Enqueue(componentIndex);
            _entityAssignments[componentIndex] = ECS.InvalidEntityId;
            _entityIdToBufferIndex.Remove(entityId);
            return true;
        }

        public ComponentIterator<TComponent> GetIterator() => new ComponentIterator<TComponent>(this);

        internal int GetEntityAt(int index) => _entityAssignments[index];
        internal TComponent GetAt(int index) => _buffer[index];
        internal void ModifyAt(int index, ModifyDelegate<TComponent> modifyDelegate) =>
            modifyDelegate.Invoke(ref _buffer[index]);
    }

    public class BufferFullException<TComponent> : Exception where TComponent : unmanaged
    {
        public override string Message =>
            $"Unable to complete the operation, the buffer of type {typeof(TComponent)} is full.";
    }
}