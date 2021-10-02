using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class Simulation
    {
        public ISimulationConfig Config { get; }
        public Frame CurrentFrame { get; private set; }
        
        private int _entityCounter;
        private readonly Dictionary<Type, IComponentBuffer> _typeToComponentBuffers;
        private readonly Dictionary<Type, IEntityComponentIterator> _typeToEntityComponentIterator;
        private ISystem[] _systems;
        private Frame[] _frames;

        public Simulation(ISimulationConfig config, ISystem[] systems)
        {
            Config = config;
            _typeToComponentBuffers = new Dictionary<Type, IComponentBuffer>();
            _typeToEntityComponentIterator = new Dictionary<Type, IEntityComponentIterator>();
            _systems = systems;
            _frames = new Frame[config.MaxRollback];

            for (var i = 0; i < _frames.Length; i++)
                _frames[i] = new Frame(this);

            CurrentFrame = _frames[_frames.Length - 1];
        }

        public Entity CreateEntity() => new Entity(_entityCounter++);

        public void DestroyEntity(Entity entity)
        {
            if (entity.IsDestroyed)
                return;

            foreach (var type in entity.TypeToComponentIndexes.Keys)
                RemoveComponent(type, entity);
            
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

        public void RemoveComponent<TComponent>(Entity entity) where TComponent : unmanaged => RemoveComponent(typeof(TComponent), entity);

        private void RemoveComponent(Type type, Entity entity)
        {
            var buffer = _typeToComponentBuffers[type];
            buffer.Release(entity[type]);
            var iterator = _typeToEntityComponentIterator[type];
            iterator.RemoveEntity(entity);
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
            var prevFrame = CurrentFrame;
            CurrentFrame = _frames[(prevFrame.Number + 1) % _frames.Length];
            CurrentFrame.Reset(prevFrame);

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