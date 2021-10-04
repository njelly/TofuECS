using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class Simulation
    {
        public ISimulationConfig Config { get; }
        public Frame CurrentFrame { get; private set; }
        public int LastVerifiedFrame { get; private set; }

        internal Dictionary<Type, IComponentBuffer> TypeToComponentBuffers { get; }
        internal Dictionary<Type, IEntityComponentIterator> TypeToEntityComponentIterator { get; }

        private int _entityCounter;
        private ISystem[] _systems;
        private Frame[] _frames;

        public Simulation(ISimulationConfig config, ISystem[] systems)
        {
            Config = config;
            TypeToComponentBuffers = new Dictionary<Type, IComponentBuffer>();
            TypeToEntityComponentIterator = new Dictionary<Type, IEntityComponentIterator>();
            _systems = systems;
            _frames = new Frame[config.MaxRollback];

            for (var i = 0; i < _frames.Length; i++)
                _frames[i] = new Frame(this);

            CurrentFrame = _frames[_frames.Length - 1];
        }

        public Entity CreateEntity() => new Entity(_entityCounter++);

        public void RegisterComponent<TComponent>() where TComponent : unmanaged
        {
            var buffer = new ComponentBuffer<TComponent>();
            TypeToComponentBuffers.Add(typeof(TComponent), buffer);
            TypeToEntityComponentIterator.Add(typeof(TComponent), new EntityComponentIterator<TComponent>(buffer));
        }

        public void Tick()
        {
            var prevFrame = CurrentFrame;
            CurrentFrame = _frames[(prevFrame.Number + 1) % _frames.Length];
            CurrentFrame.Recycle(prevFrame);

            if (Config.Mode != SimulationMode.Client)
            {
                VerifyFrame(CurrentFrame);
                LastVerifiedFrame = CurrentFrame.Number;
            }

            foreach (var system in _systems)
                system.Process(CurrentFrame);
        }

        private void VerifyFrame(Frame f)
        {
            f.Verify();
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