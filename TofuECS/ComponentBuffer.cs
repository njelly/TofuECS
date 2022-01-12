using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public delegate void ModifyDelegate<TComponent>(ref TComponent component) where TComponent : unmanaged;
    public unsafe delegate void ModifyDelegateUnsafe<TComponent>(TComponent* component) where TComponent : unmanaged;
    
    public class ComponentBuffer<TComponent> where TComponent : unmanaged
    {
        public int Size => _buffer.Length;

        private readonly TComponent[] _buffer;
        private readonly int[] _entityAssignments;
        private readonly Queue<int> _freeIndexes;
        private readonly Dictionary<int, int> _entityToIndex;

        internal ComponentBuffer(int size)
        {
            _buffer = new TComponent[size];
            _entityAssignments = new int[size];
            _freeIndexes = new Queue<int>(size);
            _entityToIndex = new Dictionary<int, int>();

            for (var i = size - 1; i >= 0; i--)
            {
                _entityAssignments[i] = Simulation.InvalidEntityId;
                _freeIndexes.Enqueue(i);
            }
        }

        public bool Get(int entityId, out TComponent component)
        {
            if (_entityToIndex.TryGetValue(entityId, out var componentIndex))
            {
                component = _buffer[componentIndex];
                return true;
            }

            component = default;
            return false;
        }

        public bool GetAndModify(int entityId, ModifyDelegate<TComponent> modifyDelegate)
        {
            if (!_entityToIndex.TryGetValue(entityId, out var componentIndex)) 
                return false;
            
            modifyDelegate.Invoke(ref _buffer[componentIndex]);
            return true;
        }

        public unsafe bool GetAndModifyUnsafe(int entityId, ModifyDelegateUnsafe<TComponent> modifyDelegateUnsafe)
        {
            if (!_entityToIndex.TryGetValue(entityId, out var componentIndex)) 
                return false;

            fixed (TComponent* ptr = &_buffer[componentIndex])
                modifyDelegateUnsafe.Invoke(ptr);

            return true;
        }

        public void Set(int entityId, in TComponent component)
        {
            if (_entityToIndex.TryGetValue(entityId, out var componentIndex))
            {
                _buffer[componentIndex] = component;
                return;
            }
            
            if (_freeIndexes.Count <= 0)
                throw new BufferFullException<TComponent>();
            
            componentIndex = _freeIndexes.Dequeue();
            _entityToIndex.Add(entityId, componentIndex);
            _entityAssignments[componentIndex] = entityId;
            _buffer[componentIndex] = component;
        }

        public void Set(int entityId)
        {
            if (_entityToIndex.TryGetValue(entityId, out var componentIndex))
            {
                _buffer[componentIndex] = new TComponent();
                return;
            }
            
            if (_freeIndexes.Count <= 0)
                throw new BufferFullException<TComponent>();
            
            componentIndex = _freeIndexes.Dequeue();
            _entityToIndex.Add(entityId, componentIndex);
            _entityAssignments[componentIndex] = entityId;
            _buffer[componentIndex] = new TComponent();
        }

        public bool Remove(int entityId)
        {
            if(!_entityToIndex.TryGetValue(entityId, out var componentIndex))
                return false;
            
            _freeIndexes.Enqueue(componentIndex);
            _entityAssignments[componentIndex] = Simulation.InvalidEntityId;
            _entityToIndex.Remove(entityId);
            return true;
        }

        public ComponentIterator<TComponent> GetIterator() => new ComponentIterator<TComponent>(this);

        internal void SetState(TComponent[] state, int[] entityAssignments)
        {
            Array.Copy(_buffer, state, Math.Min(state.Length, _buffer.Length));
            Array.Copy(_entityAssignments, entityAssignments,
                Math.Min(entityAssignments.Length, _entityAssignments.Length));
            
            _freeIndexes.Clear();
            _entityToIndex.Clear();

            for (var i = _entityAssignments.Length - 1; i >= 0; i--)
            {
                if (_entityAssignments[i] == Simulation.InvalidEntityId)
                    _freeIndexes.Enqueue(i);
                else
                    _entityToIndex.Add(_entityAssignments[i], i);
            }
        }

        internal void GetState(out TComponent[] state, out int[] entityAssignments)
        {
            state = _buffer;
            entityAssignments = _entityAssignments;
        }

        internal bool GetFirst(out TComponent component)
        {
            for (var i = 0; i < _buffer.Length; i++)
            {
                if (_entityAssignments[i] == Simulation.InvalidEntityId) 
                    continue;
                
                component = _buffer[i];
                return true;
            }

            component = default;
            return false;
        }
        
        internal bool ModifyFirst(ModifyDelegate<TComponent> modifyDelegate)
        {
            for (var i = 0; i < _buffer.Length; i++)
            {
                if (_entityAssignments[i] == Simulation.InvalidEntityId) 
                    continue;

                modifyDelegate(ref _buffer[i]);
                return true;
            }

            return false;
        }

        internal unsafe bool ModifyFirstUnsafe(ModifyDelegateUnsafe<TComponent> modifyDelegateUnsafe)
        {
            for (var i = 0; i < _buffer.Length; i++)
            {
                if (_entityAssignments[i] == Simulation.InvalidEntityId) 
                    continue;

                fixed (TComponent* ptr = &_buffer[i])
                    modifyDelegateUnsafe(ptr);
                
                return true;
            }

            return false;
        }

        internal int GetEntityAt(int index) => _entityAssignments[index];
        internal TComponent GetAt(int index) => _buffer[index];
        internal void ModifyAt(int index, ModifyDelegate<TComponent> modifyDelegate) =>
            modifyDelegate.Invoke(ref _buffer[index]);
        internal unsafe void ModifyAtUnsafe(int index, ModifyDelegateUnsafe<TComponent> modifyDelegateUnsafe)
        {
            fixed (TComponent* ptr = &_buffer[index])
                modifyDelegateUnsafe(ptr);
        }
    }

    public class BufferFullException<TComponent> : Exception where TComponent : unmanaged
    {
        public override string Message =>
            $"Unable to complete the operation, the buffer of type {typeof(TComponent)} is full.";
    }
}