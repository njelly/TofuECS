using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tofunaut.TofuECS
{
    public class Frame
    {
        public int Number { get; private set; }

        private readonly Simulation _sim;

        public Frame(Simulation sim)
        {
            _sim = sim;
            Number = -1;
        }

        public void DestroyEntity(Entity entity)
        {
            if (entity.IsDestroyed)
                return;

            foreach (var type in entity.TypeToComponentIndexes.Keys)
                RemoveComponent(type, entity);

            entity.Destroy();
        }

        public void AddComponent<TComponent>(Entity entity) where TComponent : unmanaged
        {
            entity.AssignComponent(typeof(TComponent), _sim.TypeToComponentBuffers[typeof(TComponent)].Request());
            _sim.TypeToEntityComponentIterator[typeof(TComponent)].AddEntity(entity);
        }

        public void RemoveComponent<TComponent>(Entity entity) where TComponent : unmanaged => RemoveComponent(typeof(TComponent), entity);

        private void RemoveComponent(Type type, Entity entity)
        {
            var buffer = _sim.TypeToComponentBuffers[type];
            buffer.Release(entity[type]);
            var iterator = _sim.TypeToEntityComponentIterator[type];
            iterator.RemoveEntity(entity);
        }

        public TComponent GetComponent<TComponent>(Entity entity) where TComponent : unmanaged
        {
            try
            {
                var buffer = (ComponentBuffer<TComponent>)_sim.TypeToComponentBuffers[typeof(TComponent)];
                unsafe
                {
                    return buffer.Get(entity[typeof(TComponent)]);
                }
            }
            catch
            {
                throw new EntityDoesNotContainComponentException<TComponent>(entity);
            }
        }

        public unsafe TComponent* GetComponentUnsafe<TComponent>(Entity entity) where TComponent : unmanaged
        {
            try
            {
                var buffer = (ComponentBuffer<TComponent>)_sim.TypeToComponentBuffers[typeof(TComponent)];
                return buffer.GetUnsafe(entity[typeof(TComponent)]);
            }
            catch
            {
                throw new EntityDoesNotContainComponentException<TComponent>(entity);
            }
        }

        public bool TryGetComponent<TComponent>(Entity entity, out TComponent component) where TComponent : unmanaged
        {
            if (!entity.TypeToComponentIndexes.TryGetValue(typeof(TComponent), out var index))
            {
                component = default;
                return false;
            }
            var buffer = (ComponentBuffer<TComponent>)_sim.TypeToComponentBuffers[typeof(TComponent)];
            unsafe
            {
                component = buffer.Get(index);
                return true;
            }
        }

        public unsafe bool TryGetComponentUnsafe<TComponent>(Entity entity, out TComponent* component) where TComponent : unmanaged
        {
            if (!entity.TypeToComponentIndexes.TryGetValue(typeof(TComponent), out var index))
            {
                component = null;
                return false;
            }
            var buffer = (ComponentBuffer<TComponent>)_sim.TypeToComponentBuffers[typeof(TComponent)];
            component = buffer.GetUnsafe(index);
            return true;
        }

        public EntityComponentIterator<TComponent> GetIterator<TComponent>() where TComponent : unmanaged
        {
            var iterator = (EntityComponentIterator<TComponent>)_sim.TypeToEntityComponentIterator[typeof(TComponent)];
            iterator.Reset();
            return iterator;
        }

        public void Reset(Frame prevFrame)
        {
            Number = prevFrame.Number + 1;
        }
    }
}
