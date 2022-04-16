using System;
using UnsafeCollections.Collections.Unsafe;

namespace Tofunaut.TofuECS
{
    internal unsafe class EntityComponentListBuffer<TComponent> : EntityComponentBuffer<TComponent> where TComponent : unmanaged
    {
        public override int Size => UnsafeList.GetCount(_list);
        
        private readonly UnsafeList* _list;
        
        public EntityComponentListBuffer(int size) : base(size)
        {
            _list = UnsafeList.Allocate<TComponent>(size);
            for(var i = 0; i < size; i++)
                UnsafeList.Add(_list, default(TComponent));
        }

        public override bool Get(int entityId, out TComponent component)
        {
            if (_entityToIndex.TryGetValue(entityId, out var componentIndex))
            {
                component = UnsafeList.Get<TComponent>(_list, componentIndex);
                return true;
            }

            component = default;
            return false;
        }

        public override TComponent Get(int entityId)
        {
            try
            {
                return UnsafeList.Get<TComponent>(_list, _entityToIndex[entityId]);
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
                component = UnsafeList.GetPtr<TComponent>(_list, componentIndex);
                return true;
            }

            component = null;
            return false;
        }

        public override TComponent* GetUnsafe(int entityId)
        {
            try
            {
                return UnsafeList.GetPtr<TComponent>(_list, _entityToIndex[entityId]);
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
                UnsafeList.Set(_list, componentIndex, component);
                return;
            }
            
            componentIndex = _freeIndexes.Count > 0 ? _freeIndexes.Dequeue() : _entityAssignments.Count;
            _entityToIndex.Add(entityId, componentIndex);
            _entityAssignments[componentIndex] = entityId;
            UnsafeList.Set(_list, componentIndex, component);
            base.Set(entityId);
        }

        public override void Set(int entityId)
        {
            if (_entityToIndex.TryGetValue(entityId, out var componentIndex))
            {
                UnsafeList.Set(_list, componentIndex, new TComponent());
                return;
            }
            
            componentIndex = _freeIndexes.Count > 0 ? _freeIndexes.Dequeue() : _entityAssignments.Count;
            _entityToIndex.Add(entityId, componentIndex);
            _entityAssignments[componentIndex] = entityId;
            UnsafeList.Set(_list, componentIndex, new TComponent());
            base.Set(entityId);
        }

        internal override void SetState(TComponent[] state, int[] entityAssignments)
        {
            UnsafeList.Clear(_list);
            foreach (var component in state)
                UnsafeList.Add(_list, component);
            
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
                UnsafeList.CopyTo<TComponent>(_list, statePtr, 0);
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
            component = UnsafeList.Get<TComponent>(_list, i);
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
            component = UnsafeList.GetPtr<TComponent>(_list, i);
            i++;
            return true;
        }

        public override void Dispose()
        {
            UnsafeList.Free(_list);
        }
    }
}