using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class Simulation
    {
        public int TickNumber { get; private set; } = -1;
        public ISimulationConfig Config { get; }
        
        private int _entityCounter;
        private readonly Dictionary<Type, IComponentBuffer> _typeToComponentBuffers;
        private readonly Dictionary<Type, IEntityComponentIterator> _typeToEntityComponentIterator;
        private ISystem[] _systems;

        public Simulation(ISimulationConfig config, ISystem[] systems)
        {
            Config = config;
            _typeToComponentBuffers = new Dictionary<Type, IComponentBuffer>();
            _typeToEntityComponentIterator = new Dictionary<Type, IEntityComponentIterator>();
            _systems = systems;
        }

        public Entity CreateEntity() => new Entity(_entityCounter++);

        public void DestroyEntity(Entity entity)
        {
            if (entity.IsDestroyed)
                return;

            foreach (var pair in entity.TypeToComponentIndexes)
            {
                var buffer = _typeToComponentBuffers[pair.Key];
                buffer.Release(pair.Value);
                var iterator = _typeToEntityComponentIterator[pair.Key];
                iterator.RemoveEntity(entity);
            }
            
            entity.Destroy();
        }

        public void RegisterComponent<TComponent>() where TComponent : unmanaged
        {
            var buffer = new ComponentBuffer<TComponent>();
            _typeToComponentBuffers.Add(typeof(TComponent), buffer);
            _typeToEntityComponentIterator.Add(typeof(TComponent), new EntityComponentIterator<TComponent>(buffer));
        }

        public void AddComponent<TComponent>(Entity entity) where TComponent : unmanaged
        {
            entity.AssignComponent(typeof(TComponent), _typeToComponentBuffers[typeof(TComponent)].Request());
            _typeToEntityComponentIterator[typeof(TComponent)].AddEntity(entity);
        }

        public TComponent GetComponent<TComponent>(Entity entity) where TComponent : unmanaged
        {
            try
            {
                var buffer = (ComponentBuffer<TComponent>)_typeToComponentBuffers[typeof(TComponent)];
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
                var buffer = (ComponentBuffer<TComponent>)_typeToComponentBuffers[typeof(TComponent)];
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
            var buffer = (ComponentBuffer<TComponent>)_typeToComponentBuffers[typeof(TComponent)];
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
            var buffer = (ComponentBuffer<TComponent>)_typeToComponentBuffers[typeof(TComponent)];
            component = buffer.GetUnsafe(index);
            return true;
        }

        public EntityComponentIterator<TComponent> GetIterator<TComponent>() where TComponent : unmanaged
        {
            var iterator = (EntityComponentIterator<TComponent>)_typeToEntityComponentIterator[typeof(TComponent)];
            iterator.Reset();
            return iterator;
        }

        public void Tick()
        {
            TickNumber++;

            foreach (var system in _systems)
                system.Process(this);
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