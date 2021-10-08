using System;

namespace Tofunaut.TofuECS
{
    public class Frame
    {
        public int Number { get; private set; }
        public bool IsVerified { get; private set; }

        private readonly Simulation _sim;
        private IComponentBuffer[] _componentBuffers;
        private IEntityComponentIterator[] _iterators;

        public Frame(Simulation sim)
        {
            _sim = sim;
            Number = -1;
            _componentBuffers = new IComponentBuffer[0];
            _iterators = new IEntityComponentIterator[0];
        }

        public void DestroyEntity(Entity entity)
        {
            entity.Destroy(Number);
        }

        public void AddComponent<TComponent>(Entity entity) where TComponent : unmanaged
        {
            var typeIndex = _sim.GetIndexForType(typeof(TComponent));
            entity.AssignComponent(typeof(TComponent), Number, IsVerified, _componentBuffers[typeIndex].Request());
            _iterators[typeIndex].AddEntity(entity);
        }

        public void RemoveComponent<TComponent>(Entity entity) where TComponent : unmanaged => RemoveComponent(typeof(TComponent), entity);

        private void RemoveComponent(Type type, Entity entity)
        {
            if (!entity.TryGetComponentIndex(type, out var index))
                return;

            var typeIndex = _sim.GetIndexForType(type);
            _componentBuffers[typeIndex].Release(index);
            _iterators[typeIndex].RemoveEntity(entity);
        }

        public TComponent GetComponent<TComponent>(Entity entity) where TComponent : unmanaged
        {
            if (!entity.TryGetComponentIndex(typeof(TComponent), out var index))
                throw new EntityDoesNotContainComponentException<TComponent>(entity);


            var typeIndex = _sim.GetIndexForType(typeof(TComponent));
            return ((ComponentBuffer<TComponent>)_componentBuffers[typeIndex]).Get(index);
        }

        public unsafe TComponent* GetComponentUnsafe<TComponent>(Entity entity) where TComponent : unmanaged
        {
            if (!entity.TryGetComponentIndex(typeof(TComponent), out var index))
                throw new EntityDoesNotContainComponentException<TComponent>(entity);

            var typeIndex = _sim.GetIndexForType(typeof(TComponent));
            return ((ComponentBuffer<TComponent>)_componentBuffers[typeIndex]).GetUnsafe(index);
        }

        public bool TryGetComponent<TComponent>(Entity entity, out TComponent component) where TComponent : unmanaged
        {
            if (!entity.TryGetComponentIndex(typeof(TComponent), out var index))
            {
                component = default;
                return false;
            }

            var typeIndex = _sim.GetIndexForType(typeof(TComponent));
            component = ((ComponentBuffer<TComponent>)_componentBuffers[typeIndex]).Get(index);
            return true;
        }

        public unsafe bool TryGetComponentUnsafe<TComponent>(Entity entity, out TComponent* component) where TComponent : unmanaged
        {
            if (!entity.TryGetComponentIndex(typeof(TComponent), out var index))
            {
                component = default;
                return false;
            }

            var typeIndex = _sim.GetIndexForType(typeof(TComponent));
            component = ((ComponentBuffer<TComponent>)_componentBuffers[typeIndex]).GetUnsafe(index);
            return true;
        }

        public EntityComponentIterator<TComponent> GetIterator<TComponent>() where TComponent : unmanaged
        {
            var typeIndex = _sim.GetIndexForType(typeof(TComponent));
            var iterator = (EntityComponentIterator<TComponent>)_iterators[typeIndex];
            iterator.Reset();
            iterator.buffer = (ComponentBuffer<TComponent>)_componentBuffers[typeIndex];
            return iterator;
        }

        internal void Recycle(Frame prevFrame)
        {
            Number = prevFrame.Number + 1;
            IsVerified = false;

            for (var i = 0; i < prevFrame._componentBuffers.Length; i++)
            {
                _componentBuffers[i].CopyFrom(prevFrame._componentBuffers[i]);
                _iterators[i].CopyFrom(prevFrame._iterators[i]);
            }
        }

        internal void Verify()
        {
            IsVerified = true;
        }

        internal void RegisterComponent<TComponent>() where TComponent : unmanaged
        {
            var newComponentBufferArray = new IComponentBuffer[_componentBuffers.Length + 1];
            Array.Copy(_componentBuffers, newComponentBufferArray, _componentBuffers.Length);
            _componentBuffers = newComponentBufferArray;
            var newComponentBuffer = new ComponentBuffer<TComponent>();
            _componentBuffers[_componentBuffers.Length - 1] = newComponentBuffer;

            var newIteratorArray = new IEntityComponentIterator[_iterators.Length + 1];
            Array.Copy(_iterators, newIteratorArray, _iterators.Length);
            _iterators = newIteratorArray;
            _iterators[_iterators.Length - 1] = new EntityComponentIterator<TComponent>();
        }
    }

    public class EntityDoesNotContainComponentException<TComponent> : Exception where TComponent : unmanaged
    {
        private readonly Entity _entity;
        public EntityDoesNotContainComponentException(Entity entity)
        {
            _entity = entity;
        }
        public override string Message => $"the entity {_entity.Id} does not contain the component {nameof(TComponent)}";
    }
}
