using System;
using UnsafeCollections.Collections.Unsafe;

namespace Tofunaut.TofuECS
{
    /// <summary>
    /// A buffer containing the state information for the ECS. Array implementation.
    /// </summary>
    /// <typeparam name="TComponent">An unmanaged component type.</typeparam>
    internal unsafe class EntityComponentArrayBuffer<TComponent> : EntityComponentBuffer<TComponent> where TComponent : unmanaged
    {
        public override int Size => UnsafeArray.GetLength(_arr);
        
        private readonly UnsafeArray* _arr;

        internal EntityComponentArrayBuffer(int size) : base(size)
        {
            _arr = UnsafeArray.Allocate<TComponent>(size);
        }
        
        public override bool Get(int entityId, out TComponent component)
        {
            if (_entityToIndex.TryGetValue(entityId, out var componentIndex))
            {
                component = UnsafeArray.Get<TComponent>(_arr, componentIndex);
                return true;
            }

            component = default;
            return false;
        }
        
        public override TComponent Get(int entityId)
        {
            try
            {
                return UnsafeArray.Get<TComponent>(_arr, _entityToIndex[entityId]);
            }
            catch
            {
                throw new EntityNotAssignedException<TComponent>(entityId);
            }
        }
        
        public override bool GetUnsafe(int entityId, out TComponent* component)
        {
            if (_entityToIndex.TryGetValue(entityId, out var componentIndex))
            {
                component = UnsafeArray.GetPtr<TComponent>(_arr, componentIndex);
                return true;
            }

            component = null;
            return false;
        }
        
        public override TComponent* GetUnsafe(int entityId)
        {
            try
            {
                return UnsafeArray.GetPtr<TComponent>(_arr, _entityToIndex[entityId]);
            }
            catch
            {
                throw new EntityNotAssignedException<TComponent>(entityId);
            }
        }
        
        public override void Set(int entityId, in TComponent component)
        {
            if (_entityToIndex.TryGetValue(entityId, out var componentIndex))
            {
                UnsafeArray.Set(_arr, componentIndex, component);
                return;
            }
            
            if (_freeIndexes.Count <= 0)
                throw new BufferFullException<TComponent>();
            
            componentIndex = _freeIndexes.Dequeue();
            _entityToIndex.Add(entityId, componentIndex);
            _entityAssignments[componentIndex] = entityId;
            UnsafeArray.Set(_arr, componentIndex, component);
            base.Set(entityId);
        }
        
        public override void Set(int entityId)
        {
            if (_entityToIndex.TryGetValue(entityId, out var componentIndex))
            {
                UnsafeArray.Set(_arr, componentIndex, new TComponent());
                return;
            }
            
            if (_freeIndexes.Count <= 0)
                throw new BufferFullException<TComponent>();
            
            componentIndex = _freeIndexes.Dequeue();
            _entityToIndex.Add(entityId, componentIndex);
            _entityAssignments[componentIndex] = entityId;
            UnsafeArray.Set(_arr, componentIndex, new TComponent());
            base.Set(entityId);
        }
        
        internal override void SetState(TComponent[] state, int[] entityAssignments)
        {
            fixed (TComponent* statePtr = state)
                _arr->CopyFrom<TComponent>(statePtr, 0, Math.Min(state.Length, Size));
            
            _entityAssignments.Clear();
            foreach (var entityAssignment in entityAssignments)
                _entityAssignments.Add(entityAssignment);
            
            _freeIndexes.Clear();
            _entityToIndex.Clear();

            for (var i = _entityAssignments.Count - 1; i >= 0; i--)
            {
                if (_entityAssignments[i] == Simulation.InvalidEntityId)
                    _freeIndexes.Enqueue(i);
                else
                    _entityToIndex.Add(_entityAssignments[i], i);
            }
        }
        
        public override void GetState(TComponent[] state)
        {
            fixed (TComponent* statePtr = state)
                _arr->CopyTo<TComponent>(statePtr, 0);
        }
        
        public override bool Next(ref int i, out int entityId, out TComponent component)
        {
            while (i < Size && _entityAssignments[i] == Simulation.InvalidEntityId)
                i++;

            if (i >= Size)
            {
                entityId = Simulation.InvalidEntityId;
                component = default;
                return false;
            }

            entityId = _entityAssignments[i];
            component = UnsafeArray.Get<TComponent>(_arr, i);
            i++;
            return true;
        }
        
        public override bool NextUnsafe(ref int i, out int entityId, out TComponent* component)
        {
            while (i < Size && _entityAssignments[i] == Simulation.InvalidEntityId)
                i++;

            if (i >= Size)
            {
                entityId = Simulation.InvalidEntityId;
                component = default;
                return false;
            }

            entityId = _entityAssignments[i];
            component = UnsafeArray.GetPtr<TComponent>(_arr, i);
            i++;
            return true;
        }

        public override void Dispose()
        {
            UnsafeArray.Free(_arr);
        }
    }
}